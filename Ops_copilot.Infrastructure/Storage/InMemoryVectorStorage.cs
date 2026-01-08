
namespace Ops_copilot.Infrastructure.Storage;

using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Embeddings;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;

public class InMemoryVectorStore : IInMemoryVectorDatabase
{
    private readonly IEmbeddingGenerationService<string, float> _embeddingService;
    private readonly ConcurrentBag<Record> _store = new();

    private sealed record Record(string Id, ReadOnlyMemory<float> Embedding, string Text, string Description, string AdditionalMetadata);

    public InMemoryVectorStore(IEmbeddingGenerationService<string, float> embeddingService)
    {
        _embeddingService = embeddingService;
    }
    public async Task<Result<Guid>> SaveDocumentAsync(Document document, CancellationToken ct = default)
    {
        try
        {
            foreach (var chunk in document.Chunks)
            {
                string metadataString = $"docId:{document.Id}|page:{chunk.PageNumber}";

#pragma warning disable CS0618
                var embeddingResult = await _embeddingService.GenerateEmbeddingAsync<string, float>(
                    chunk.Content,
                    kernel: null,          // optional
                    cancellationToken: ct  // cancellation token
                );
#pragma warning restore CS0618

                var vector = embeddingResult;


                _store.Add(new Record(
                    Id: chunk.Id.ToString(),
                    Embedding: vector,
                    Text: chunk.Content,
                    Description: document.FileName,
                    AdditionalMetadata: metadataString
                ));
            }
            return Result<Guid>.Success(document.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(new Error("VectorStore.SaveError", ex.Message));
        }
    }

    public Task<Result<List<DocumentChunk>>> SearchSimilarAsync(ReadOnlyMemory<float> queryEmbedding, int limit = 5, CancellationToken ct = default)
    {
        try
        {
            var scored = _store.Select(r => (r, Score: CosineSimilarity(queryEmbedding.Span, r.Embedding.Span)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .ToList();

            var chunks = scored.Select(x =>
            {
                var (docId, page) = ParseMetadata(x.r.AdditionalMetadata);
                return new DocumentChunk(
                    Content: x.r.Text,
                    PageNumber: page,
                    SequenceNumber: 0,
                    DocumentId: docId,
                    Id: Guid.TryParse(x.r.Id, out var cid) ? cid : Guid.NewGuid(),
                    Embedding: x.r.Embedding
                );
            }).ToList();

            return Task.FromResult(Result<List<DocumentChunk>>.Success(chunks));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<DocumentChunk>>.Failure(new Error("VectorStore.SearchError", ex.Message)));
        }
    }

    private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length) return 0f;
        double dot = 0; double na = 0; double nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0f;
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }

    private static (Guid docId, int page) ParseMetadata(string metadata)
    {
        if (string.IsNullOrEmpty(metadata)) return (Guid.Empty, 0);
        var parts = metadata.Split('|');
        Guid docId = Guid.Empty;
        int page = 0;
        foreach (var part in parts)
        {
            if (part.StartsWith("docId:") && Guid.TryParse(part.Split(':')[1], out var g)) docId = g;
            else if (part.StartsWith("page:") && int.TryParse(part.Split(':')[1], out var p)) page = p;
        }
        return (docId, page);
    }
}