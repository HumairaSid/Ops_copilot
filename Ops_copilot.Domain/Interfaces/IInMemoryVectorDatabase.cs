namespace Ops_copilot.Domain.Interfaces;

using Ops_copilot.Domain.Common;
public interface IInMemoryVectorDatabase
{
    Task<Result<Guid>> SaveDocumentAsync(Document document, CancellationToken ct = default);
    Task<Result<IEnumerable<DocumentChunk>>> GetDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<List<DocumentChunk>>> SearchSimilarAsync(ReadOnlyMemory<float> queryEmbedding, Guid? documentId = null, int limit = 5, CancellationToken ct = default);
}

