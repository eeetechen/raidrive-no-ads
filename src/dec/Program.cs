using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Metadata;

// 用法:
//   dec <input.dll> [--out <file>] [--type <substring>] [--ref <dir>]...
//
// 例:
//   dec RaiDrive.Mount.dll --out out.cs
//   dec RaiDrive.Mount.dll --type AdvertisingViewModel --ref ./bundle --ref ./shared
class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: dec <input.dll> [--out <file>] [--type <substring>] [--ref <dir>]...");
            return 2;
        }

        string path = args[0];
        string outFile = null;
        string typeFilter = null;
        var refDirs = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":
                    if (++i >= args.Length) { Console.Error.WriteLine("--out needs a value"); return 2; }
                    outFile = args[i];
                    break;
                case "--type":
                    if (++i >= args.Length) { Console.Error.WriteLine("--type needs a value"); return 2; }
                    typeFilter = args[i];
                    break;
                case "--ref":
                    if (++i >= args.Length) { Console.Error.WriteLine("--ref needs a value"); return 2; }
                    refDirs.Add(args[i]);
                    break;
                default:
                    Console.Error.WriteLine($"unknown argument: {args[i]}");
                    return 2;
            }
        }

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"input not found: {path}");
            return 1;
        }

        var settings = new DecompilerSettings
        {
            LocalFunctions = false,          // 规避 ILSpy 9 在局部函数上的崩溃
            AnonymousMethods = true,
            YieldReturn = true,
            AsyncAwait = true,
            ShowXmlDocumentation = false,
        };

        var resolver = new UniversalAssemblyResolver(path, false, "net10.0");
        resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        // 依赖程序集（比如从 bundle 里抽出来的那 48 个 dll）通过 --ref 传入。
        // 以前这里写死了一个本机绝对路径，换台机器就编译不过，也会泄露用户名。
        foreach (var dir in refDirs)
        {
            if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine($"warning: reference dir not found: {dir}");
                continue;
            }
            resolver.AddSearchDirectory(Path.GetFullPath(dir));
        }

        var decompiler = new CSharpDecompiler(path, resolver, settings);
        var output = new StringWriter();

        foreach (var t in decompiler.TypeSystem.MainModule.TypeDefinitions.OrderBy(t => t.FullName))
        {
            if (typeFilter != null && !t.FullName.Contains(typeFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                output.WriteLine(decompiler.DecompileTypeAsString(new FullTypeName(t.FullName)));
                output.WriteLine();
            }
            catch (Exception e)
            {
                output.WriteLine($"// !! failed {t.FullName}: {e.GetType().Name}: {e.Message}");
                // 整类反编译失败时，退化成逐个成员反编译
                foreach (var m in t.Members)
                {
                    try
                    {
                        output.WriteLine(decompiler.DecompileAsString(m.MetadataToken));
                    }
                    catch (Exception e2)
                    {
                        output.WriteLine($"// !! {t.FullName}::{m.Name} {e2.GetType().Name}");
                    }
                }
            }
        }

        var text = output.ToString();
        if (outFile != null)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(outFile));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(outFile, text);
            Console.Error.WriteLine($"written: {outFile} ({text.Length:N0} chars)");
        }
        else
        {
            Console.Write(text);
        }
        return 0;
    }
}
