using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        var path = args[0];
        var mode = args.Length > 1 ? args[1] : "info";
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();

        if (mode == "iltext") { IlText.Dump(path, args[2], args[3]); return; }

        if (mode == "rows") { RowInfo.Dump(path, args[2]); return; }

        if (mode == "whybad") { WhyBad.Run(path, int.Parse(args[2])); return; }

        if (mode == "sigs") { Sigs.Run(path, args.Skip(2).ToArray()); return; }

        if (mode == "verify") { Verify.Run(path); return; }

        if (mode == "stats") { Stats.Run(path); return; }

        if (mode == "res")
        {
            Resources.Dump(path, args.Length > 2 ? args[2] : "res_out");
            return;
        }

        if (mode == "info")
        {
            Console.WriteLine($"types={md.TypeDefinitions.Count} methods={md.MethodDefinitions.Count} fields={md.FieldDefinitions.Count}");
            foreach (var th in md.TypeDefinitions)
            {
                var t = md.GetTypeDefinition(th);
                string tn = Full(md, t);
                foreach (var mh in t.GetMethods())
                {
                    var m = md.GetMethodDefinition(mh);
                    if (m.RelativeVirtualAddress == 0) continue;
                    var ib = pe.GetMethodBody(m.RelativeVirtualAddress);
                    if (ib.Size > 200) Console.WriteLine($"{ib.Size,7} {tn}::{md.GetString(m.Name)}");
                }
            }
            return;
        }

        // mode=callgraph: show type + method sizes and their member references
        if (mode == "refs")
        {
            string filter = args[2];
            foreach (var th in md.TypeDefinitions)
            {
                var t = md.GetTypeDefinition(th);
                string tn = Full(md, t);
                if (!tn.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var mh in t.GetMethods())
                {
                    var m = md.GetMethodDefinition(mh);
                    Console.WriteLine($"\n== {tn}::{md.GetString(m.Name)} (rva={m.RelativeVirtualAddress})");
                    if (m.RelativeVirtualAddress == 0) { Console.WriteLine("   <no body>"); continue; }
                    var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                    var il = body.GetILBytes();
                    Console.WriteLine($"   il={il.Length} bytes  maxstack={body.MaxStack} localsInit={body.LocalVariablesInitialized}");
                    var ilr = body.GetILReader();
                    while (ilr.RemainingBytes > 0)
                    {
                        int off = ilr.Offset;
                        int op = ilr.ReadByte();
                        if (op == 0xFE) { op = 0xFE00 | ilr.ReadByte(); }
                        var opc = OpCodeTable.From(op);
                        string operand = "";
                        var kind = opc.OperandType;
                        switch (kind)
                        {
                            case "ShortInlineI": operand = ((sbyte)ilr.ReadByte()).ToString(); break;
                            case "ShortInlineVar": operand = "loc"+ilr.ReadByte(); break;
                            case "InlineVar": operand = "loc"+ilr.ReadUInt16(); break;
                            case "ShortInlineBrTarget": operand = "IL_" + (ilr.Offset + 1 + (sbyte)ilr.ReadByte()).ToString("x4"); break;
                            case "InlineBrTarget": operand = "IL_" + (ilr.Offset + 4 + ilr.ReadInt32()).ToString("x4"); break;
                            case "InlineI": operand = ilr.ReadInt32().ToString(); break;
                            case "InlineI8": operand = ilr.ReadInt64().ToString(); break;
                            case "ShortInlineR": operand = ilr.ReadSingle().ToString("R"); break;
                            case "InlineR": operand = ilr.ReadDouble().ToString("R"); break;
                            case "InlineString":
                            {
                                var h = MetadataTokens.UserStringHandle(ilr.ReadInt32() & 0x00FFFFFF);
                                operand = "\"" + md.GetUserString(h).Replace("\n","\\n") + "\"";
                                break;
                            }
                            case "InlineMethod":
                            {
                                var h = MetadataTokens.MethodDefinitionHandle(ilr.ReadInt32() & 0x00FFFFFF);
                                operand = ResolveMethod(md, h);
                                break;
                            }
                            case "InlineField":
                            {
                                var h = MetadataTokens.FieldDefinitionHandle(ilr.ReadInt32() & 0x00FFFFFF);
                                operand = ResolveField(md, h);
                                break;
                            }
                            case "InlineType":
                            {
                                var h = MetadataTokens.TypeDefinitionHandle(ilr.ReadInt32() & 0x00FFFFFF);
                                operand = ResolveType(md, h);
                                break;
                            }
                            case "InlineTok": operand = $"tok(0x{ilr.ReadInt32():x8})"; break;
                            case "InlineSig": operand = $"sig(0x{ilr.ReadInt32():x8})"; break;
                            case "InlineSwitch":
                            {
                                uint n = ilr.ReadUInt32();
                                var targets = new List<string>();
                                for (int i = 0; i < n; i++) { int d = ilr.ReadInt32(); targets.Add((ilr.Offset + d).ToString("x4")); }
                                operand = "[" + string.Join(",", targets) + "]";
                                break;
                            }
                        }
                        if (off < 4096) Console.WriteLine($"   IL_{off:x4}: {opc.Name,-12} {operand}");
                    }
                }
            }
            return;
        }
    }
    static string Full(MetadataReader md, TypeDefinition t)
    {
        string ns = md.GetString(t.Namespace), n = md.GetString(t.Name);
        return string.IsNullOrEmpty(ns) ? n : ns + "." + n;
    }
    static string ResolveMethod(MetadataReader md, MethodDefinitionHandle h)
    {
        try { var m = md.GetMethodDefinition(h); var t = md.GetTypeDefinition(m.GetDeclaringType()); return Full(md, t) + "::" + md.GetString(m.Name); }
        catch { return "method(0x" + MetadataTokens.GetToken(h).ToString("x8") + ")"; }
    }
    static string ResolveField(MetadataReader md, FieldDefinitionHandle h)
    {
        try { var f = md.GetFieldDefinition(h); var t = md.GetTypeDefinition(f.GetDeclaringType()); return Full(md, t) + "::" + md.GetString(f.Name); }
        catch { return "field(0x" + MetadataTokens.GetToken(h).ToString("x8") + ")"; }
    }
    static string ResolveType(MetadataReader md, TypeDefinitionHandle h)
    {
        try { return Full(md, md.GetTypeDefinition(h)); }
        catch { return "type(0x" + MetadataTokens.GetToken(h).ToString("x8") + ")"; }
    }
}
