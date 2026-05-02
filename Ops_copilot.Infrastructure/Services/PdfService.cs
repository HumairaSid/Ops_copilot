using Ops_copilot.Domain.Common;
using Ops_copilot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using Microsoft.SemanticKernel.Text;

namespace Ops_copilot.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;


    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;

    }

    public async Task<Result<List<DocumentChunk>>> ExtractTextAsync(Stream pdfStream, Guid documentId, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var allChunks = new List<DocumentChunk>();

                using var document = PdfDocument.Open(pdfStream);

                foreach (var page in document.GetPages())
                {
                    if (ct.IsCancellationRequested) break;

                    var pageText = page.Text;
                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        _logger.LogDebug("Page {PageNumber} has no text", page.Number);
                        continue;
                    }

                    // Use token-aware chunking for this page's text
                    var pageChunks = TextChunker.SplitPlainTextLines(pageText, 300);
                    // _chunker.ChunkText(pageText, documentId, maxTokensPerChunk: 500, overlapTokens: 50);

                    // Update page number for each chunk
                    foreach (var chunk in pageChunks)
                    {
                        allChunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            Content = chunk,
                            PageNumber = page.Number,
                            SequenceNumber = allChunks.Count + 1,
                            DocumentId = documentId
                        });

                    }
                }

                _logger.LogInformation(
                    "Extracted {ChunkCount} chunks from PDF (document {DocumentId})",
                    allChunks.Count, documentId);

                return Result<List<DocumentChunk>>.Success(allChunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF extraction failed for document {DocumentId}", documentId);
                return Result<List<DocumentChunk>>.Failure(new Error("Pdf.ExtractionError", ex.Message));
            }
        }, ct);
    }
}