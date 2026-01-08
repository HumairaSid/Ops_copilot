namespace Ops_copilot.Domain.Common;
public record DocumentChunk(
    string Content,
        int PageNumber,
    int SequenceNumber,
      Guid DocumentId,
    Guid Id = default,
      ReadOnlyMemory<float>? Embedding = null

);
