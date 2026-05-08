namespace Ops_copilot.Domain.Common;

public record DocumentChunk
{
    public Guid Id { get; init; } = default;
    public string Content { get; init; } = string.Empty;
    public int PageNumber { get; init; }
    public int SequenceNumber { get; init; }
    public Guid DocumentId { get; init; }
    public ReadOnlyMemory<float>? Embedding { get; init; }

}

