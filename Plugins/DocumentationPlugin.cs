using System.ComponentModel;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.Model;

namespace SemanticKernelPlayground.Plugins;
public class DocumentationPlugin
{
    [KernelFunction, Description("Scans the codebase and generates documentation chunks for each file.")]
    public List<DocumentationChunk> GenerateDocumentation(string rootPath)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[DEBUG] Scanning codebase at {rootPath}");
        var docs = new List<DocumentationChunk>();
        foreach (var file in Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            Console.WriteLine($"[DEBUG] Processing file: {file}");
            var text = File.ReadAllText(file);
            var chunks = CodeChunker.ChunkFile(file, text);
            docs.AddRange(chunks);
        }

        Console.WriteLine($"[DEBUG] Generated {docs.Count} documentation chunks.");
        return docs;
    }
}