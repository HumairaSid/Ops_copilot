
namespace Ops_copilot.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.AI;
using Ops_copilot.Application;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;

public class SemanticAIService : ISemanticAIService
{
    private readonly Kernel _kernel;
    private readonly IInMemoryVectorDatabase _vectorDb;
    private readonly IInMemoryDocumentStore _docStore;
    private readonly ILogger<SemanticAIService> _logger;

    public SemanticAIService(
        Kernel kernel,
        IInMemoryVectorDatabase vectorDb,
        IInMemoryDocumentStore docStore,
        ILogger<SemanticAIService> logger)
    {
        _kernel = kernel;
        _vectorDb = vectorDb;
        _docStore = docStore;
        _logger = logger;
    }

    // 1. INDEXING BLOCK
    public async Task<Result<Guid>> IndexDocumentChunksAsync(Document document, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Indexing document {DocumentId} into vector store", document.Id);

            // Passes the document to the vector database implementation
            // The VectorDB will handle converting text to embeddings and storing them
            return await _vectorDb.SaveDocumentAsync(document, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed");
            return Result<Guid>.Failure(new Error("AI.IndexError", "Failed to save document to vector store."));
        }
    }

    // 2. SUMMARIZATION BLOCK
    public async Task<Result<string>> SummarizeDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        // Fetch raw text from the document store
        var docResult = await _docStore.GetByIdAsync(documentId, ct);
        if (docResult.IsFailure) return Result<string>.Failure(docResult.Error);

        var fullText = string.Join("\n", docResult.Value!.Chunks.Select(c => c.Content));

        var prompt = $"""
            Summarize the following document content concisely. 
            Highlight the primary purpose and three key takeaways.
            ---
            {fullText}
            """;

        try
        {
            // Invoke the LLM (Ollama) using Semantic Kernel's prompt engine
            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            return Result<string>.Success(response.ToString());
        }
        catch (Exception)
        {
            return Result<string>.Failure(new Error("AI.SummaryError", "LLM failed to generate summary."));
        }
    }

    // 3. RAG (QUESTION & ANSWER) BLOCK
    public async Task<Result<string>> AnswerQuestionAsync(Guid documentId, string question, CancellationToken ct = default)
    {
        try
        {
            // A. Convert the user's string question into a vector (Embedding)
#pragma warning disable CS0618
            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerationService<string, float>>();

            var embeddingResult = await embeddingService.GenerateEmbeddingAsync<string, float>(
                question,
                kernel: null,          // optional Kernel, can be null
                cancellationToken: ct  // CancellationToken
            );
#pragma warning restore CS0618

            ReadOnlyMemory<float> questionEmbedding = embeddingResult;

            // B. Retrieval: Ask the Vector DB for the top 5 most relevant text chunks
            var searchResult = await _vectorDb.SearchSimilarAsync(questionEmbedding, limit: 5, ct: ct);
            if (searchResult.IsFailure) return Result<string>.Failure(searchResult.Error);

            var context = string.Join("\n", searchResult.Value!.Select(c => c.Content));

            // C. Augmentation: Construct a prompt that includes the retrieved context
            var prompt = $"""
            Use the following pieces of context to answer the user's question.
            If you don't know the answer, just say that you don't know, don't try to make up an answer.
            
            Context:
            {context}
            
            Question: {question}
            """;

            // D. Generation: Send the augmented prompt to the LLM
            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            return Result<string>.Success(response.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG pipeline failed");
            return Result<string>.Failure(new Error("AI.RagError", "Failed to retrieve or generate an answer."));
        }
    }
}