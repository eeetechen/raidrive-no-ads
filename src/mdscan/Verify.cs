using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

static class Verify
{
    public static void Run(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        Console.WriteLine($"MethodDefs={md.MethodDefinitions.Count}");

        // 1. every method body must parse
        int bad = 0, ok = 0;
        var ilReader = pe.GetMetadataReader();
        foreach (var mh in md.MethodDefinitions)
        {
            var m = md.GetMethodDefinition(mh);
            if (m.RelativeVirtualAddress == 0) continue;
            try { pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes(); ok++; }
            catch { bad++; }
        }
        Console.WriteLine($"bodies ok={ok} bad={bad}");
        if (bad > 0) return;

        // 2. validate every method's IL structurally using System.Reflection.Metadata's
        //    ILReader walk (catches truncated opcodes / bad operands)
        int ilBad = 0;
        foreach (var mh in md.MethodDefinitions)
        {
            var m = md.GetMethodDefinition(mh);
            if (m.RelativeVirtualAddress == 0) continue;
            var body = pe.GetMethodBody(m.RelativeVirtualAddress);
            var il = body.GetILBytes();
            try { Walk(il); }
            catch (Exception e) { if (ilBad++ < 5) Console.WriteLine($"  IL walk fail rva=0x{m.RelativeVirtualAddress:x}: {e.Message}"); }
        }
        Console.WriteLine($"IL walk failures={ilBad}");
    }

    static void Walk(byte[] il)
    {
        int p = 0;
        while (p < il.Length)
        {
            byte op = il[p++];
            bool op2 = false;
            if (op == 0xFE) { if (p >= il.Length) throw new Exception("truncated 2-byte opcode"); op = il[p++]; op2 = true; }
            int code = op;
            if (op2) code = 0xFE00 | op;
            string kind = OpCodeTable.From(code).OperandType;
            switch (kind)
            {
                case "None": break;
                case "ShortInlineI": case "ShortInlineVar": case "ShortInlineBrTarget": p += 1; break;
                case "InlineVar": case "InlineBrTarget": p += 2; break;
                case "ShortInlineR": p += 4; break;
                case "InlineI": case "InlineR": case "InlineString": case "InlineMethod":
                case "InlineField": case "InlineType": case "InlineTok": case "InlineSig": case "InlineBrTarget2":
                    p += 4; break;
                case "InlineI8": p += 8; break;
                case "InlineSwitch":
                    if (p + 4 > il.Length) throw new Exception("switch truncated");
                    uint n = BitConverter.ToUInt32(il, p); p += 4 + (int)n*4; break;
                default: break;
            }
            if (p > il.Length) throw new Exception($"overrun at {p} > {il.Length}");
        }
    }
}
