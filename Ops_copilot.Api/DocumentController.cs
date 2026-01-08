using Microsoft.AspNetCore.Mvc;
using Ops_copilot.Application;
using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;

namespace Ops_copilot.API.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IInMemoryDocumentStore _docStore;
        private readonly ISemanticAIService _aiService;

        public DocumentController(
            IPdfService pdfService,
            IInMemoryDocumentStore docStore,
            ISemanticAIService aiService)
        {
            _pdfService = pdfService;
            _docStore = docStore;
            _aiService = aiService;
        }

        // 1️⃣ Upload & Process PDF
        [HttpPost("upload")]
        [DisableRequestSizeLimit] // optional, for large files
        public async Task<IActionResult> UploadDocument(
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            using var stream = file.OpenReadStream();
            var documentId = Guid.NewGuid();

            var extractResult = await _pdfService.ExtractTextAsync(stream, documentId, ct);
            if (extractResult.IsFailure)
                return Problem(extractResult.Error.Message);

            var document = new Document
            {
                Id = documentId,
                FileName = file.FileName
            };
            document.AddChunks(extractResult.Value!);

            await _docStore.UpsertAsync(document, ct);

            var indexResult = await _aiService.IndexDocumentChunksAsync(document, ct);
            if (indexResult.IsFailure)
                return Problem(indexResult.Error.Message);

            return Ok(new { DocumentId = documentId, ChunksProcessed = document.Chunks.Count });
        }

        // 2️⃣ Summarize Document
        [HttpGet("{id:guid}/summary")]
        public async Task<IActionResult> SummarizeDocument(Guid id, CancellationToken ct)
        {
            var result = await _aiService.SummarizeDocumentAsync(id, ct);
            if (result.IsFailure) return NotFound(result.Error.Message);
            return Ok(result.Value);
        }

        // 3️⃣ Q&A (RAG)
        [HttpPost("{id:guid}/ask")]
        public async Task<IActionResult> AskDocumentQuestion(
            Guid id,
            [FromBody] string question,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(question))
                return BadRequest("Question is required.");

            var result = await _aiService.AnswerQuestionAsync(id, question, ct);
            if (result.IsFailure) return Problem(result.Error.Message);
            var response = new AnswerResponse
            {
                Answer = result.Value!
            };
            return Ok(response);
        }
    }
}
