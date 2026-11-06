using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

static class RowInfo
{
    public static void Dump(string path, string outFile)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        using var w = new StreamWriter(outFile);
        for (int r = 1; r <= md.MethodDefinitions.Count; r++)
        {
            var h = MetadataTokens.MethodDefinitionHandle(r);
            var m = md.GetMethodDefinition(h);
            w.WriteLine($"{r}\t{m.RelativeVirtualAddress}\t{md.GetString(m.Name)}");
        }
    }
}
