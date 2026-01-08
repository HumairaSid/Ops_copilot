
namespace Ops_copilot.Infrastructure.Storage;


using System.Collections.Concurrent;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;



public class InMemoryDocumentStore : IInMemoryDocumentStore
{
    private static readonly ConcurrentDictionary<Guid, Document> _store = new();

    public Task<Result<Document>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_store.TryGetValue(id, out var document))
            return Task.FromResult(Result<Document>.Success(document));

        return Task.FromResult(Result<Document>.Failure(new Error("Store.NotFound", "Document not found.")));
    }

    public Task<Result<Guid>> UpsertAsync(Document document, CancellationToken ct = default)
    {
        _store[document.Id] = document;
        return Task.FromResult(Result<Guid>.Success(document.Id));
    }

    public Task<Result<bool>> ExistsAsync(string fileName, CancellationToken ct = default)
    {
        // 1. Check for cancellation
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<Result<bool>>(ct);

        // 2. Perform the check (case-insensitive for file names is usually best)
        var exists = _store.Values.Any(d =>
            d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        // 3. Return a successful Result wrap
        return Task.FromResult(Result<bool>.Success(exists));
    }
}