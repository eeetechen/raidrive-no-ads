using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

static class WhyBad
{
    public static void Run(string path, int row)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var h = MetadataTokens.MethodDefinitionHandle(row);
        var m = md.GetMethodDefinition(h);
        Console.WriteLine($"row {row} rva=0x{m.RelativeVirtualAddress:x} localsig nil={m.RelativeVirtualAddress==0}");
        var body = pe.GetMethodBody(m.RelativeVirtualAddress);
        Console.WriteLine($"maxstack={body.MaxStack} localsInit={body.LocalVariablesInitialized} localsig={MetadataTokens.GetToken(body.LocalSignature):x8}");
        var il = body.GetILBytes();
        Console.WriteLine($"il={il.Length}");
        // print exception regions
        foreach (var r in body.ExceptionRegions)
            Console.WriteLine($"  EH {r.Kind} try {r.TryOffset:x4}+{r.TryLength} handler {r.HandlerOffset:x4}+{r.HandlerLength} catch={MetadataTokens.GetToken(r.CatchType):x8}");
        // walk the IL and report the first structural problem
        int p = 0;
        while (p < il.Length)
        {
            int at = p;
            byte op = il[p++];
            int code = op;
            if (op == 0xFE) { code = 0xFE00 | il[p++]; }
            string k = OpCodeTable.From(code).OperandType;
            Console.Write($"{at,4}: {OpCodeTable.From(code).Name}");
            switch (k)
            {
                case "None": break;
                case "ShortInlineI": p += 1; break;
                case "ShortInlineVar": p += 1; break;
                case "ShortInlineBrTarget": p += 1; break;
                case "InlineVar": p += 2; break;
                case "InlineBrTarget": p += 4; break;
                case "ShortInlineR": p += 4; break;
                case "InlineI": p += 4; break;
                case "InlineR": p += 8; break;
                case "InlineI8": p += 8; break;
                case "InlineString": case "InlineMethod": case "InlineField":
                case "InlineType": case "InlineTok": case "InlineSig": p += 4; break;
                case "InlineSwitch":
                    uint n = BitConverter.ToUInt32(il, p); p += 4 + (int)n * 4; break;
            }
            Console.WriteLine();
            if (p > il.Length) { Console.WriteLine($"  >> OVERRUN at {at}"); return; }
        }
    }
}
