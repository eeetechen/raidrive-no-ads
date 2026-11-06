using System.Linq;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class Validate
{
    public static void Run(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        if (!pe.HasMetadata) { Console.WriteLine("no metadata"); return; }
        var md = pe.GetMetadataReader();
        Console.WriteLine($"types={md.TypeDefinitions.Count} methods={md.MethodDefinitions.Count}");
        int bad = 0, checked_ = 0;
        foreach (var mh in md.MethodDefinitions)
        {
            var m = md.GetMethodDefinition(mh);
            if (m.RelativeVirtualAddress == 0) continue;
            checked_++;
            try
            {
                var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                var il = body.GetILBytes();
            }
            catch (Exception e)
            {
                if (bad++ < 5) Console.WriteLine($"  bad rva=0x{m.RelativeVirtualAddress:x}: {e.GetType().Name}");
            }
        }
        Console.WriteLine($"checked={checked_} bad={bad}");
        // also exercise the type system (this is what throws Bad IL format most often)
        try
        {
            var dr = new ICSharpCode.Decompiler.Metadata.PEFile(path);
            var ts = new ICSharpCode.Decompiler.TypeSystem.DecompilerTypeSystem(dr,
                new ICSharpCode.Decompiler.Metadata.UniversalAssemblyResolver(path, false, "net10.0"));
            Console.WriteLine($"type system ok, types={ts.MainModule.TypeDefinitions.Count()}");
        }
        catch (Exception e) { Console.WriteLine($"TYPE SYSTEM FAIL: {e.GetType().Name}: {e.Message}"); }
    }
}
