using Ops_copilot.Domain.Common;

namespace Ops_copilot.Application;

public interface ISemanticAIService
{
    /// <summary>
    /// Orchestrates the process of converting a Document's chunks into 
    /// embeddings and storing them in the Vector Database.
    /// </summary>
    Task<Result<Guid>> IndexDocumentChunksAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a document from the store and uses the LLM to provide 
    /// a high-level summary.
    /// </summary>
    Task<Result<string>> SummarizeDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Performs a Retrieval-Augmented Generation (RAG) query. 
    /// 1. Embeds the question. 2. Finds relevant chunks. 3. Asks LLM for answer.
    /// </summary>
    Task<Result<string>> AnswerQuestionAsync(Guid documentId, string question, CancellationToken ct = default);
}