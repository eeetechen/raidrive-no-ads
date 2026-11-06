using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class Resources
{
    public static void Dump(string path, string outDir)
    {
        Directory.CreateDirectory(outDir);
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        Console.WriteLine($"manifest resources: {md.ManifestResources.Count}");
        foreach (var rh in md.ManifestResources)
        {
            var r = md.GetManifestResource(rh);
            string name = md.GetString(r.Name);
            string vis = r.Attributes.ToString();
            if (!r.Implementation.IsNil)
            {
                Console.WriteLine($"  [in FILE] {name} ({vis})");
                continue;
            }
            var dir = pe.PEHeaders.CorHeader.ResourcesDirectory;
            int rva = dir.RelativeVirtualAddress + (int)r.Offset;
            var sec = pe.GetSectionData(rva);
            // read 4-byte length then bytes
            var s = pe.GetSectionData(rva).GetReader();
            int len = s.ReadInt32();
            var buf = new byte[len];
            var reader = pe.GetSectionData(rva + 4).GetReader();
            reader.ReadBytes(len, buf, 0);
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            File.WriteAllBytes(Path.Combine(outDir, safe), buf);
            Console.WriteLine($"  {name} ({vis}) size={len}");
        }
    }
}
