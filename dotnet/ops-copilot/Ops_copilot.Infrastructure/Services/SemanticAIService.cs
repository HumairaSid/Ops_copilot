using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Ops_copilot.Application;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;

namespace Ops_copilot.Infrastructure.Services;

/// <summary>
/// Orchestrates RAG operations: document ingestion, chunking, embedding, and Q&A.
/// </summary>
public class SemanticAIService : ISemanticAIService
{
    private readonly Kernel _kernel;
    private readonly IInMemoryVectorDatabase _vectorDb;
    private readonly IPdfService _pdfService;
    private readonly ILogger<SemanticAIService> _logger;

    public SemanticAIService(
        Kernel kernel,
        IInMemoryVectorDatabase vectorDb,
        IPdfService pdfService,
        ILogger<SemanticAIService> logger)
    {
        _kernel = kernel;
        _vectorDb = vectorDb;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Complete document ingestion pipeline:
    /// 1. Extract text from PDF
    /// 2. Generate embeddings for chunks
    /// 3. Store in vector database
    /// </summary>
    public async Task<Result<Guid>> ProcessDocumentAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        var documentId = Guid.NewGuid();

        _logger.LogInformation("Starting document processing: {FileName} ({DocumentId})", fileName, documentId);

        // Step 1: Extract text and chunk from PDF
        var extractResult = await _pdfService.ExtractTextAsync(fileStream, documentId, ct);
        if (extractResult.IsFailure)
        {
            _logger.LogError("Failed to extract text from PDF: {Error}", extractResult.Error);
            return Result<Guid>.Failure(extractResult.Error);
        }

        var chunks = extractResult.Value!;
        _logger.LogInformation("Extracted {ChunkCount} chunks from {FileName}", chunks.Count, fileName);

        // Step 2: Create document entity and index chunks (generate embeddings + store)
        var document = new Document
        {
            Id = documentId,
            FileName = fileName,
            ContentType = "application/pdf",
            SizeInBytes = fileStream.Length,
            CreatedAt = DateTime.UtcNow
        };
        document.AddChunks(chunks);

        var indexResult = await IndexDocumentChunksAsync(document, ct);
        if (indexResult.IsFailure)
        {
            return Result<Guid>.Failure(indexResult.Error);
        }

        _logger.LogInformation("Document processing complete: {DocumentId}", documentId);
        return Result<Guid>.Success(documentId);
    }

    /// <summary>
    /// Converts document chunks to embeddings and stores them in the vector database.
    /// </summary>
    public async Task<Result<Guid>> IndexDocumentChunksAsync(Document document, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Indexing document {DocumentId} into vector store", document.Id);

            if (document.Chunks.Count == 0)
            {
                return Result<Guid>.Failure(new Error("AI.NoChunks", "Document has no chunks to index."));
            }

            // Get embedding service from Semantic Kernel
            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var chunksWithEmbeddings = new List<DocumentChunk>();

            foreach (var chunk in document.Chunks)
            {
                // Generate embedding for the chunk content
                var embeddingResult = await embeddingService.GenerateAsync(
                    new List<string> { chunk.Content },
                    cancellationToken: ct);

                var embedding = embeddingResult.FirstOrDefault() ??
                    throw new InvalidOperationException($"Failed to generate embedding for chunk {chunk.Id}");

                chunksWithEmbeddings.Add(new DocumentChunk
                {
                    Id = chunk.Id != default ? chunk.Id : Guid.NewGuid(),
                    Content = chunk.Content,
                    PageNumber = chunk.PageNumber,
                    SequenceNumber = chunk.SequenceNumber,
                    DocumentId = document.Id,
                    Embedding = embedding.Vector
                });
            }

            // Create a new document with chunks that have embeddings
            var documentWithEmbeddings = new Document
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                SizeInBytes = document.SizeInBytes,
                CreatedAt = document.CreatedAt
            };
            documentWithEmbeddings.AddChunks(chunksWithEmbeddings);

            // Store in vector database
            return await _vectorDb.SaveDocumentAsync(documentWithEmbeddings, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed for document {DocumentId}", document.Id);
            return Result<Guid>.Failure(new Error("AI.IndexError", "Failed to save document to vector store."));
        }
    }

    /// <summary>
    /// Generates a high-level summary of a document using the LLM.
    /// </summary>
    public async Task<Result<string>> SummarizeDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating summary for document {DocumentId}", documentId);

        // Fetch document chunks from the vector store
        var docResult = await _vectorDb.GetDocumentAsync(documentId, ct);
        if (docResult.IsFailure)
        {
            _logger.LogWarning("Document not found for summarization: {DocumentId}", documentId);
            return Result<string>.Failure(docResult.Error);
        }

        var fullText = string.Join("\n\n", docResult.Value!.Select(c => c.Content));

        if (string.IsNullOrWhiteSpace(fullText))
        {
            return Result<string>.Failure(new Error("AI.NoContent", "Document has no content to summarize."));
        }

        try
        {
            var arguments = new KernelArguments
            {
                { "input", fullText },
                { "targetLanguage", "English" },
                { "maxLength", "200" }
            };

            var response = await _kernel.InvokePromptAsync(PromptTemplates.SummarizeTemplate, arguments, cancellationToken: ct);
            var summary = response.ToString();
            _logger.LogInformation("Summary generated for document {DocumentId}", documentId);
            return Result<string>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary generation failed for document {DocumentId}", documentId);
            return Result<string>.Failure(new Error("AI.SummaryError", "LLM failed to generate summary."));
        }
    }

    /// <summary>
    /// RAG-based question answering: retrieves relevant chunks and generates an answer.
    /// </summary>
    public async Task<Result<string>> AnswerQuestionAsync(Guid documentId, string question, CancellationToken ct = default)
    {
        _logger.LogInformation("Answering question for document {DocumentId}: {Question}", documentId, question);

        try
        {
            // Step 1: Embed the question
            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var questionEmbeddingResult = await embeddingService.GenerateAsync(
                new List<string> { question },
                cancellationToken: ct);

            var questionEmbedding = questionEmbeddingResult.FirstOrDefault()?.Vector ??
                throw new InvalidOperationException("Failed to generate embedding for question");

            // Step 2: Retrieve top 5 most similar chunks for this document only
            var searchResult = await _vectorDb.SearchSimilarAsync(questionEmbedding, documentId, limit: 5, ct: ct);
            if (searchResult.IsFailure)
            {
                return Result<string>.Failure(searchResult.Error);
            }

            var context = string.Join("\n\n", searchResult.Value!.Select(c => c.Content));

            if (string.IsNullOrWhiteSpace(context))
            {
                return Result<string>.Failure(new Error("AI.NoContext", "No relevant context found for this question."));
            }

            // Step 3: Generate answer using RAG prompt
            var arguments = new KernelArguments
            {
                { "context", context },
                { "question", question }
            };

            var response = await _kernel.InvokePromptAsync(PromptTemplates.RagAskTemplate, arguments, cancellationToken: ct);
            var answer = response.ToString();
            _logger.LogInformation("Answer generated for document {DocumentId}", documentId);
            return Result<string>.Success(answer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG pipeline failed for document {DocumentId}", documentId);
            return Result<string>.Failure(new Error("AI.RagError", "Failed to retrieve or generate an answer."));
        }
    }
}
