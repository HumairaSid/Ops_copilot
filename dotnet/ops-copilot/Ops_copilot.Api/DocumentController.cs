using Microsoft.AspNetCore.Mvc;
using Ops_copilot.Application;
using Ops_copilot.Domain.Common;

namespace Ops_copilot.API.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly ISemanticAIService _aiService;

        public DocumentController(ISemanticAIService aiService)
        {
            _aiService = aiService;
        }

        // 1️⃣ Upload & Process PDF
        [HttpPost("upload")]
        [RequestSizeLimit(2 * 1024 * 1024)] // Limit to 2MB
        public async Task<IActionResult> UploadDocument(
            [FromForm] IFormFile file,
            CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest("Max file size is 2 MB");

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are supported.");

            using var stream = file.OpenReadStream();
            var result = await _aiService.ProcessDocumentAsync(stream, file.FileName, ct);

            if (result.IsFailure)
                return BadRequest(result.Error.Message);

            return Ok(new { documentId = result.Value });
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
            [FromBody] AskQuestionRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required.");

            var result = await _aiService.AnswerQuestionAsync(id, request.Question, ct);
            if (result.IsFailure) return Problem(result.Error.Message);
            return Ok(new { answer = result.Value });
        }
        [HttpGet("test")]
        public async Task<IActionResult> Testservice()
        {

            return Ok("Service is up and running!");
        }
    }
}
