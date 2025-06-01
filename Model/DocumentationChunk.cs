using Microsoft.Extensions.VectorData;

namespace SemanticKernelPlayground.Model;

public record DocumentationChunk
{
    [VectorStoreRecordKey]
    public required string Id { get; init; }
    [VectorStoreRecordData]
    public required string FileName { get; init; }
    [VectorStoreRecordData]
    public required int ChunkIndex { get; init; }
    [VectorStoreRecordData]
    public required string Content { get; init; }
    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; init; }
}