using SemanticKernelPlayground.Model;

namespace SemanticKernelPlayground;

public static class CodeChunker
{
    // Splits code into chunks by class or method for simplicity
    public static List<DocumentationChunk> ChunkFile(string fileName, string fileContent)
    {
        var chunks = new List<DocumentationChunk>();
        var lines = fileContent.Split('\n');
        var currentChunk = new List<string>();
        int chunkIndex = 0;
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("class ") || line.Trim().StartsWith("public ") || line.Trim().StartsWith("private "))
            {
                if (currentChunk.Count > 0)
                {
                    chunks.Add(new DocumentationChunk
                    {
                        Id = $"{fileName}#{chunkIndex}",
                        FileName = fileName,
                        ChunkIndex = chunkIndex++,
                        Content = string.Join("\n", currentChunk)
                    });
                    currentChunk.Clear();
                }
            }
            currentChunk.Add(line);
        }
        if (currentChunk.Count > 0)
        {
            chunks.Add(new DocumentationChunk
            {
                Id = $"{fileName}#{chunkIndex}",
                FileName = fileName,
                ChunkIndex = chunkIndex++,
                Content = string.Join("\n", currentChunk)
            });
        }
        return chunks;
    }
}