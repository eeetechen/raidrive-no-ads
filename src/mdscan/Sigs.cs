using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class Sigs
{
    public static void Run(string path, string[] names)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        foreach (var h in md.MethodDefinitions)
        {
            var m = md.GetMethodDefinition(h);
            string n = md.GetString(m.Name);
            if (!names.Contains(n)) continue;
            var t = md.GetTypeDefinition(m.GetDeclaringType());
            string tn = md.GetString(t.Namespace) + "." + md.GetString(t.Name);
            string sig = "";
            try
            {
                var blob = md.GetBlobReader(m.Signature);
                sig = DescribeSignature(md, blob);
            }
            catch (Exception e) { sig = "<" + e.GetType().Name + ">"; }
            int rva = m.RelativeVirtualAddress;
            Console.WriteLine($"row {MetadataTokensGetRow(h)}  {tn}::{n}");
            Console.WriteLine($"    sig: {sig}   rva=0x{rva:x}");
            if (rva != 0)
            {
                try
                {
                    var b = pe.GetMethodBody(rva);
                    var il = b.GetILBytes();
                    Console.WriteLine($"    il={il.Length}B  first={BitConverter.ToString(il.Take(6).ToArray())}");
                }
                catch { Console.WriteLine("    <il unreadable>"); }
            }
        }
    }
    static int MetadataTokensGetRow(MethodDefinitionHandle h)
    {
        int v = System.Reflection.Metadata.Ecma335.MetadataTokens.GetRowNumber(h);
        return v;
    }
    static string DescribeSignature(MetadataReader md, BlobReader r)
    {
        var header = r.ReadSignatureHeader();
        var sb = new System.Text.StringBuilder();
        sb.Append(header.CallingConvention.ToString());
        if (header.IsGeneric) sb.Append($"`{r.ReadCompressedInteger()}");
        int n = r.ReadCompressedInteger();
        var ret = ReadType(md, ref r);
        var ps = new System.Collections.Generic.List<string>();
        for (int i = 0; i < n; i++) ps.Add(ReadType(md, ref r));
        return $"{(header.IsInstance ? "instance " : "")}{ret} ({string.Join(", ", ps)})";
    }
    static string ReadType(MetadataReader md, ref BlobReader r)
    {
        var code = r.ReadSignatureTypeCode();
        switch (code)
        {
            case SignatureTypeCode.Void: return "void";
            case SignatureTypeCode.Boolean: return "bool";
            case SignatureTypeCode.Int32: return "int";
            case SignatureTypeCode.String: return "string";
            case SignatureTypeCode.Object: return "object";
            case SignatureTypeCode.Single: return "float";
            case SignatureTypeCode.Double: return "double";
            case SignatureTypeCode.Int16: return "short";
            case SignatureTypeCode.Byte: return "byte";
            case SignatureTypeCode.SByte: return "sbyte";
            case SignatureTypeCode.UInt32: return "uint";
            case SignatureTypeCode.UInt64: return "ulong";
            case SignatureTypeCode.Int64: return "long";
            case SignatureTypeCode.IntPtr: return "IntPtr";
            case SignatureTypeCode.UIntPtr: return "UIntPtr";
            case SignatureTypeCode.Char: return "char";
            case SignatureTypeCode.TypeHandle:
            {
                var eh = r.ReadTypeHandle();
                return eh.Kind switch
                {
                    HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)eh).Name),
                    HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)eh).Name),
                    _ => "typeref"
                };
            }
            case SignatureTypeCode.SZArray: return ReadType(md, ref r) + "[]";
            case SignatureTypeCode.ByReference: return "ref " + ReadType(md, ref r);
            case SignatureTypeCode.GenericMethodParameter:
            case SignatureTypeCode.GenericTypeParameter: return "!" + r.ReadCompressedInteger();
            case SignatureTypeCode.GenericTypeInstance:
            {
                var inner = ReadType(md, ref r);
                int argc = r.ReadCompressedInteger();
                var args = new System.Collections.Generic.List<string>();
                for (int i = 0; i < argc; i++) args.Add(ReadType(md, ref r));
                return $"{inner}<{string.Join(",", args)}>";
            }
            case SignatureTypeCode.Pointer: return ReadType(md, ref r) + "*";
            case SignatureTypeCode.TypedReference: return "TypedReference";
            default: return code.ToString();
        }
    }
}
