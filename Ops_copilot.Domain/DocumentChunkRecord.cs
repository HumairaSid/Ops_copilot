using Microsoft.Extensions.VectorData;

public sealed class DocumentChunkRecord
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; }

    [VectorStoreRecordData]
    public string Content { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public Guid DocumentId { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public string FileName { get; set; } = string.Empty;

    [VectorStoreRecordVector(Dimensions = 4096)] // Match your Ollama model dimensions (e.g., 4096 for Llama3)
    public ReadOnlyMemory<float> Embedding { get; set; }
}