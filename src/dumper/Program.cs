using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

class Program
{
    static string OutDir;
    static void Log(string s){ Console.WriteLine(s); try{ File.AppendAllText(Path.Combine(OutDir,"log.txt"), s+"\n"); }catch{} }

    static void Main(string[] a)
    {
        string target = a[0];
        OutDir = a.Length>1? a[1] : "dump";
        Directory.CreateDirectory(OutDir);

        var asm = Assembly.LoadFrom(Path.GetFullPath(target));
        var types = new List<Type>();
        try { types.AddRange(asm.GetTypes()); }
        catch (ReflectionTypeLoadException ex) { types.AddRange(ex.Types.Where(t=>t!=null)); }
        Log($"types {types.Count}");

        var all = new List<MethodBase>();
        foreach (var t in types) all.AddRange(Methods(t));
        int ok=0, fail=0;
        foreach (var m in all) { try { System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(m.MethodHandle); ok++; } catch { fail++; } }
        Log($"jitted ok={ok} fail={fail} total={all.Count}");

        // Section map from the on-disk image lets us turn each method RVA into a file
        // offset, so we can read the decrypted fat header (maxstack / localsig / flags).
        var bytes = File.ReadAllBytes(Path.GetFullPath(target));
        int peOff = BitConverter.ToInt32(bytes, 0x3c);
        int optSize = BitConverter.ToInt16(bytes, peOff + 20);
        int nSect = BitConverter.ToInt16(bytes, peOff + 6);
        int sectBase = peOff + 24 + optSize;
        var secs = new List<(int va,int vs,int rp,int rs)>();
        for (int i = 0; i < nSect; i++)
        {
            int q = sectBase + i * 40;
            secs.Add((BitConverter.ToInt32(bytes,q+12), BitConverter.ToInt32(bytes,q+8),
                      BitConverter.ToInt32(bytes,q+20), BitConverter.ToInt32(bytes,q+16)));
        }
        int RvaToOff(int rva)
        {
            foreach (var s in secs)
                if (rva >= s.va && rva < s.va + Math.Max(s.vs, s.rs)) return rva - s.va + s.rp;
            return -1;
        }

        var sb = new StringBuilder();
        int n = 0;
        foreach (var m in all.OrderBy(m => m.MetadataToken))
        {
            int rva;
            try { rva = m.MetadataToken; } catch { continue; }
            // prefer the live header (decrypted in memory), fall back to the file copy
            var body = m.GetMethodBody();
            if (body == null) continue;
            var il = body.GetILAsByteArray();
            if (il == null) continue;

            // read the header from MEMORY via the module base
            int hdrU16 = 0, maxStack = 8, localsTok = 0, initLocals = 1;
            try
            {
                var mi = m.Module.ModuleHandle;
                // use the file copy: after JIT the in-memory body may be patched
                var mdh = typeof(MethodBase).GetProperty("MetadataToken");
                int token = m.MetadataToken;
                if ((token >> 24) == 0x06)
                {
                    // find the method's RVA through the runtime's own metadata
                    int roff = RvaOfMethod(bytes, token & 0xFFFFFF);
                    int o = roff >= 0 ? RvaToOff(roff) : -1;
                    if (o >= 0)
                    {
                        // The protector rewrites the header in MEMORY when it decrypts IL,
                        // so read the live header, not the on-disk one.
                        IntPtr hinst = System.Runtime.InteropServices.Marshal.GetHINSTANCE(m.Module);
                        long baseAddr = hinst.ToInt64();
                        bool haveMem = baseAddr > 0 && baseAddr != -1;
                        if (haveMem)
                        {
                            IntPtr addr = new IntPtr(baseAddr + roff);
                            hdrU16 = System.Runtime.InteropServices.Marshal.ReadInt16(addr) & 0xFFFF;
                            if ((hdrU16 & 3) == 3)
                            {
                                maxStack = System.Runtime.InteropServices.Marshal.ReadInt16(addr, 2) & 0xFFFF;
                                localsTok = System.Runtime.InteropServices.Marshal.ReadInt32(addr, 8);
                                initLocals = ((hdrU16 >> 2) & 1);
                            }
                            else { maxStack = 8; localsTok = 0; initLocals = 0; }
                        }
                        else
                        {
                            hdrU16 = BitConverter.ToUInt16(bytes, o);
                            if ((hdrU16 & 3) == 3)
                            {
                                maxStack = BitConverter.ToUInt16(bytes, o + 2);
                                localsTok = BitConverter.ToInt32(bytes, o + 8);
                                initLocals = ((hdrU16 >> 2) & 1);
                            }
                            else { maxStack = 8; localsTok = 0; initLocals = 0; }
                        }
                    }
                }
            }
            catch { }

            sb.AppendLine($"@0x{m.MetadataToken:x8} il={il.Length} stack={maxStack} init={initLocals} loc=0x{localsTok:x8}");
            sb.AppendLine(Convert.ToHexString(il));
            n++;
        }
        File.WriteAllText(Path.Combine(OutDir, "bodies.txt"), sb.ToString());
        Log($"dumped bodies {n}");
    }

    // read MethodDef.RelativeVirtualAddress from the metadata tables
    static readonly Dictionary<string,int> _rowCache = new();
    static int RvaOfMethod(byte[] d, int row)
    {
        if (_rowCache.TryGetValue("", out int _)) { }
        // locate the MethodDef table once
        int peOff = BitConverter.ToInt32(d, 0x3c);
        int optSize = BitConverter.ToInt16(d, peOff + 20);
        int magic = BitConverter.ToUInt16(d, peOff + 24);
        int dd = peOff + 24 + (magic == 0x20b ? 112 : 96);
        int nSect = BitConverter.ToInt16(d, peOff + 6);
        int sectBase = peOff + 24 + optSize;
        var secs = new List<(int va,int vs,int rp,int rs)>();
        for (int i = 0; i < nSect; i++)
        {
            int q = sectBase + i * 40;
            secs.Add((BitConverter.ToInt32(d,q+12), BitConverter.ToInt32(d,q+8),
                      BitConverter.ToInt32(d,q+20), BitConverter.ToInt32(d,q+16)));
        }
        int R2O(int rva) { foreach (var s in secs) if (rva >= s.va && rva < s.va + Math.Max(s.vs,s.rs)) return rva - s.va + s.rp; return -1; }
        int clr = R2O(BitConverter.ToInt32(d, dd + 14*8));
        int mdRva = BitConverter.ToInt32(d, clr + 8);
        int md = R2O(mdRva);
        int verLen = BitConverter.ToInt32(d, md + 12);
        int p = md + 16 + verLen;
        int nstreams = BitConverter.ToInt16(d, p + 2);
        p += 4;
        int tables = -1;
        for (int i = 0; i < nstreams; i++)
        {
            int off = BitConverter.ToInt32(d, p), size = BitConverter.ToInt32(d, p + 4); p += 8;
            int q = p; while (d[q] != 0) q++;
            string nm = Encoding.ASCII.GetString(d, p, q - p); p = (q + 1 + 3) & ~3;
            if (nm == "#~") tables = md + off;
        }
        if (tables < 0) return -1;
        int heap = d[tables + 6];
        long valid = BitConverter.ToInt64(d, tables + 8);
        int t = tables + 24;
        var rows = new int[64];
        for (int i = 0; i < 64; i++) if ((valid & (1L << i)) != 0) { rows[i] = BitConverter.ToInt32(d, t); t += 4; }
        int strIdx = (heap & 1) != 0 ? 4 : 2;
        int guidIdx = (heap & 2) != 0 ? 4 : 2;
        int blobIdx = (heap & 4) != 0 ? 4 : 2;
        int Isz(int tb) => (tb < 64 ? rows[tb] : 0) < 0x10000 ? 2 : 4;
        int RowSize(int tb) => tb switch {
            0x00 => 2 + strIdx + guidIdx*3,
            0x01 => Isz(0x02)*2 + strIdx + strIdx,
            0x02 => 4 + strIdx*2 + Isz(0x04) + Isz(0x06),
            0x03 => Isz(0x04),
            0x04 => 2 + strIdx + blobIdx,
            0x05 => Isz(0x06),
            _ => 0 };
        int pos = t;
        for (int i = 0; i < 6; i++) if (rows[i] > 0) pos += rows[i] * RowSize(i);
        int rowSize = 4 + 2 + 2 + strIdx + blobIdx + Isz(0x08);
        if (row < 1 || row > rows[6]) return -1;
        return BitConverter.ToInt32(d, pos + (row - 1) * rowSize);
    }

    static IEnumerable<MethodBase> Methods(Type t)
    {
        const BindingFlags BF = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly;
        MethodBase[] r;
        try { r = t.GetMethods(BF).Cast<MethodBase>().Concat(t.GetConstructors(BF)).ToArray(); }
        catch { yield break; }
        foreach (var m in r) yield return m;
    }
}
