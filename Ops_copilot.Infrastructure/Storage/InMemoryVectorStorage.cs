using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;

namespace Ops_copilot.Infrastructure.Storage;

/// <summary>
/// In-memory implementation of IInMemoryVectorDatabase.
/// Stores document chunks with their embeddings and provides cosine similarity search.
/// Thread-safe for concurrent read/write operations.
/// </summary>
public class InMemoryVectorStorage : IInMemoryVectorDatabase
{
    private readonly ILogger<InMemoryVectorStorage> _logger;

    // Thread-safe storage: documentId -> list of chunks with embeddings
    private readonly ConcurrentDictionary<Guid, List<DocumentChunk>> _store;

    public InMemoryVectorStorage(ILogger<InMemoryVectorStorage> logger)
    {
        _logger = logger;
        _store = new ConcurrentDictionary<Guid, List<DocumentChunk>>();
    }

    /// <summary>
    /// Stores all chunks from a document with their generated embeddings.
    /// </summary>
    public Task<Result<Guid>> SaveDocumentAsync(Document document, CancellationToken ct = default)
    {
        try
        {
            var chunksToStore = new List<DocumentChunk>();

            foreach (var chunk in document.Chunks)
            {
                if (chunk.Embedding == null || chunk.Embedding.Value.IsEmpty)
                {
                    _logger.LogWarning("Chunk {ChunkId} has no embedding, skipping", chunk.Id);
                    continue;
                }

                chunksToStore.Add(new DocumentChunk
                {
                    Id = chunk.Id != default ? chunk.Id : Guid.NewGuid(),
                    Content = chunk.Content,
                    PageNumber = chunk.PageNumber,
                    SequenceNumber = chunk.SequenceNumber,
                    DocumentId = document.Id,
                    Embedding = chunk.Embedding
                });
            }

            if (chunksToStore.Count == 0)
            {
                return Task.FromResult(Result<Guid>.Failure(new Error("Vector.NoChunks", "No valid chunks with embeddings to store.")));
            }

            // Store or replace the document's chunks to avoid duplicates on reindex
            _store.AddOrUpdate(document.Id, chunksToStore, (_, _) => chunksToStore);

            _logger.LogInformation("Stored {ChunkCount} chunks for document {DocumentId}",
                chunksToStore.Count, document.Id);

            return Task.FromResult(Result<Guid>.Success(document.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store document {DocumentId}", document.Id);
            return Task.FromResult(Result<Guid>.Failure(new Error("Vector.StoreError", ex.Message)));

        }
    }

    public Task<Result<IEnumerable<DocumentChunk>>> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            if (_store.TryGetValue(documentId, out var storedChunks))
            {
                var result = storedChunks.Select(c => new DocumentChunk
                {
                    Content = c.Content,
                    PageNumber = c.PageNumber,
                    SequenceNumber = c.SequenceNumber,
                    DocumentId = documentId,
                    Id = c.Id,
                    Embedding = null // Don't return embeddings in this method
                });
                return Task.FromResult(Result<IEnumerable<DocumentChunk>>.Success(result));
            }
            else
            {
                return Task.FromResult(Result<IEnumerable<DocumentChunk>>.Failure(
                    new Error("Vector.NotFound", $"No document found with ID {documentId}")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document {DocumentId}", documentId);
            return Task.FromResult(Result<IEnumerable<DocumentChunk>>.Failure(
                new Error("Vector.RetrieveError", ex.Message)));
        }
    }

    /// <summary>
    /// Performs cosine similarity search to find the top-K most similar chunks.
    /// </summary>
    public Task<Result<List<DocumentChunk>>> SearchSimilarAsync(
        ReadOnlyMemory<float> queryEmbedding,
        Guid? documentId = null,
        int limit = 5,
        CancellationToken ct = default)
    {
        try
        {
            // Flatten all chunks across all documents for search
            var allChunks = _store.Values.SelectMany(chunks => chunks).ToList();

            if (allChunks.Count == 0)
            {
                return Task.FromResult(Result<List<DocumentChunk>>.Failure(
                    new Error("Vector.NoData", "No chunks available for search.")));
            }

            if (documentId.HasValue)
            {
                allChunks = allChunks.Where(chunk => chunk.DocumentId == documentId.Value).ToList();
            }

            if (allChunks.Count == 0)
            {
                return Task.FromResult(Result<List<DocumentChunk>>.Failure(
                    new Error("Vector.NotFound", "No chunks found for the requested document.")));
            }

            // Compute cosine similarity for matching chunks
            var scoredChunks = allChunks.Where(chunk => chunk.Embedding.HasValue && !chunk.Embedding!.Value.IsEmpty)
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Similarity = ComputeCosineSimilarity(queryEmbedding, chunk.Embedding!.Value)
                })
                .OrderByDescending(x => x.Similarity)
                .Take(limit)
                .ToList();

            _logger.LogDebug("Search returned {ResultCount} chunks with similarity scores",
                scoredChunks.Count);

            // Convert back to DocumentChunk
            var results = scoredChunks
                .Select(x => new DocumentChunk
                {
                    Content = x.Chunk.Content,
                    PageNumber = x.Chunk.PageNumber,
                    SequenceNumber = x.Chunk.SequenceNumber,
                    DocumentId = x.Chunk.DocumentId,
                    Id = x.Chunk.Id,
                    Embedding = null // Don't return embeddings in search results
                })
                .ToList();

            return Task.FromResult(Result<List<DocumentChunk>>.Success(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Similarity search failed");
            return Task.FromResult(Result<List<DocumentChunk>>.Failure(
                new Error("Vector.SearchError", ex.Message)));
        }
    }

    /// <summary>
    /// Computes cosine similarity between two embedding vectors.
    /// Range: -1 (opposite) to 1 (identical), with 0 meaning orthogonal.
    /// </summary>
    private static float ComputeCosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException(
                $"Embedding dimensions must match. Got {a.Length} and {b.Length}");
        }

        var spanA = a.Span;
        var spanB = b.Span;

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (var i = 0; i < spanA.Length; i++)
        {
            dotProduct += spanA[i] * spanB[i];
            magnitudeA += spanA[i] * spanA[i];
            magnitudeB += spanB[i] * spanB[i];
        }

        magnitudeA = MathF.Sqrt(magnitudeA);
        magnitudeB = MathF.Sqrt(magnitudeB);

        if (magnitudeA < float.Epsilon || magnitudeB < float.Epsilon)
        {
            return 0f; // Avoid division by zero
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Helper to find the document ID for a chunk by searching all documents.
    /// </summary>
    private Guid GetDocumentIdForChunk(Guid chunkId)
    {
        foreach (var kvp in _store)
        {
            if (kvp.Value.Any(c => c.Id == chunkId))
            {
                return kvp.Key;
            }
        }
        return Guid.Empty;
    }




}
