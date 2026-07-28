using System;
using System.Globalization;
using System.IO;

namespace PbLite.ProtoGen;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            PrintHelp();
            return 0;
        }

        string input = args[0];
        string? outputDir = null;
        string? overrideNamespace = null;
        bool multipleFiles = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --output requires a directory argument");
                        return 1;
                    }
                    outputDir = args[++i];
                    break;
                case "-n":
                case "--namespace":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --namespace requires a value");
                        return 1;
                    }
                    overrideNamespace = args[++i];
                    break;
                case "--multiple-files":
                    multipleFiles = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        outputDir ??= Directory.GetCurrentDirectory();

        try
        {
            if (Directory.Exists(input))
            {
                foreach (var protoFile in Directory.EnumerateFiles(input, "*.proto", SearchOption.AllDirectories))
                    ProcessFile(protoFile, outputDir, overrideNamespace, multipleFiles);
            }
            else if (File.Exists(input))
            {
                ProcessFile(input, outputDir, overrideNamespace, multipleFiles);
            }
            else
            {
                Console.Error.WriteLine($"Input not found: {input}");
                return 1;
            }
            return 0;
        }
        catch (ProtoParseException ex)
        {
            Console.Error.WriteLine($"Parse error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void ProcessFile(string protoPath, string outputDir, string? overrideNamespace, bool multipleFiles)
    {
        string source = File.ReadAllText(protoPath);
        var lexer = new ProtoLexer(source);
        var tokens = lexer.Tokenize();
        var parser = new ProtoParser(tokens);
        var protoFile = parser.Parse();

        var emitter = new CSharpEmitter();
        string baseName = Path.GetFileNameWithoutExtension(protoPath);
        var outputs = emitter.Emit(protoFile, baseName, overrideNamespace, multipleFiles);

        Directory.CreateDirectory(outputDir);
        foreach (var (fileName, content) in outputs)
        {
            string outputPath = Path.Combine(outputDir, fileName);
            File.WriteAllText(outputPath, content);
            Console.WriteLine($"Generated: {outputPath}");
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("PbLite.ProtoGen - proto3 to C# class generator");
        Console.WriteLine();
        Console.WriteLine("Usage: pblite-gen <input> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <input>                .proto file or directory containing .proto files");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <DIR>     Output directory (default: current directory)");
        Console.WriteLine("  -n, --namespace <NS>   Override C# namespace");
        Console.WriteLine("  --multiple-files       Generate one .cs file per message/enum");
        Console.WriteLine("  -h, --help             Show help");
    }
}
