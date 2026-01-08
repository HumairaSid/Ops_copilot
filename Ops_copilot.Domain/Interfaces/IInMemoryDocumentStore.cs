
using Ops_copilot.Domain.Common;

namespace Ops_copilot.Domain.Interfaces;

public interface IInMemoryDocumentStore
{
    Task<Result<Document>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> ExistsAsync(string fileName, CancellationToken ct = default);
    Task<Result<Guid>> UpsertAsync(Document document, CancellationToken ct = default);
}