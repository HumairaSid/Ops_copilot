
namespace Ops_copilot.Domain.Common;


public class Document
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public List<DocumentChunk> Chunks { get; private set; } = new();

    public void AddChunks(IEnumerable<DocumentChunk> chunks) => Chunks.AddRange(chunks);
}