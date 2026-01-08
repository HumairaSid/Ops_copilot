using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Ops_copilot.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;

    // RAG Best Practices: Chunks of ~500-1000 tokens work best for Ollama
    private const int MaxTokensPerLine = 100;
    private const int MaxTokensPerParagraph = 1000;
    private const int OverlapTokens = 100;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<List<DocumentChunk>>> ExtractTextAsync(Stream pdfStream, Guid documentId, CancellationToken ct = default)
    {
        // 1. Wrap the entire synchronous block in Task.Run
        return await Task.Run(() =>
        {
            try
            {
                var chunks = new List<DocumentChunk>();

                // 2. Your existing synchronous PdfPig code goes here
                using var document = PdfDocument.Open(pdfStream);

                foreach (var page in document.GetPages())
                {
                    if (ct.IsCancellationRequested) break;

                    var text = page.Text;
                    // ... your chunking/splitting logic ...

                    chunks.Add(new DocumentChunk(
                        Content: text,
                        PageNumber: page.Number,
                        SequenceNumber: chunks.Count + 1,
                        DocumentId: documentId
                    ));
                }

                return Result<List<DocumentChunk>>.Success(chunks);
            }
            catch (Exception ex)
            {
                return Result<List<DocumentChunk>>.Failure(new Error("Pdf.ExtractionError", ex.Message));
            }
        }, ct); // 3. Pass the CancellationToken to the Task.Run
    }
}