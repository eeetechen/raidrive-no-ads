using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class Stats
{
    public static void Run(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        int shown = 0;
        foreach (var h in md.MethodDefinitions)
        {
            var m = md.GetMethodDefinition(h);
            if (m.RelativeVirtualAddress == 0) continue;
            try { var b = pe.GetMethodBody(m.RelativeVirtualAddress); }
            catch (Exception ex) { if (shown++ < 5) Console.WriteLine($"FAIL rva=0x{m.RelativeVirtualAddress:x}: {ex.GetType().Name}: {ex.Message}"); }
        }
        var sizes = md.MethodDefinitions.Select(h => md.GetMethodDefinition(h))
            .Select(m => { if (m.RelativeVirtualAddress == 0) return 0; try { return pe.GetMethodBody(m.RelativeVirtualAddress).Size; } catch { return -1; } })
            .ToList();
        int n = sizes.Count(s => s > 0);
        Console.WriteLine($"{Path.GetFileName(path)}: methods={sizes.Count} withBody={n}");
        Console.WriteLine($"  <=8 bytes (stub): {sizes.Count(s => s>0 && s<=8)}  ({100.0*sizes.Count(s=>s>0&&s<=8)/n:F1}%)");
        Console.WriteLine($"  >100 bytes: {sizes.Count(s => s>100)}");
        var top = sizes.OrderByDescending(x=>x).Take(5);
        Console.WriteLine($"  top: {string.Join(",", top)}");
        Console.WriteLine($"  total IL: {sizes.Sum()}");
    }
}
