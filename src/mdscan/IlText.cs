using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

static class IlText
{
    public static void Dump(string path, string filter, string outFile)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var sb = new StringBuilder();
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            string tn = Full(md, t);
            if (!tn.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            sb.AppendLine($"\n################ {tn}");
            foreach (var mh in t.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                sb.AppendLine($"\n==== {tn}::{md.GetString(m.Name)}  rva=0x{m.RelativeVirtualAddress:x}");
                if (m.RelativeVirtualAddress == 0) { sb.AppendLine("  <no body>"); continue; }
                var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                var il = body.GetILBytes();
                sb.AppendLine($"  il={il.Length} bytes maxstack={body.MaxStack}");
                sb.AppendLine("  " + BitConverter.ToString(il).Replace("-", " "));
            }
            // fields
            foreach (var fh in t.GetFields())
            {
                var f = md.GetFieldDefinition(fh);
                sb.AppendLine($"  FIELD {md.GetString(f.Name)}  {f.Attributes}");
            }
            // nested
        }
        File.WriteAllText(outFile, sb.ToString());
        Console.WriteLine($"written {outFile} ({sb.Length} chars)");
    }

    static string Full(MetadataReader md, TypeDefinition t)
    {
        var dt = t.GetDeclaringType();
        string ns = md.GetString(t.Namespace), n = md.GetString(t.Name);
        string self = string.IsNullOrEmpty(ns) ? n : ns + "." + n;
        if (!dt.IsNil) return Full(md, md.GetTypeDefinition(dt)) + "." + n;
        return self;
    }
}
