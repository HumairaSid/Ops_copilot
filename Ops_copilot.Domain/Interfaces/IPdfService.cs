

using Ops_copilot.Domain.Common;

namespace Ops_copilot.Domain.Interfaces;

public interface IPdfService
{
   Task<Result<List<DocumentChunk>>> ExtractTextAsync(Stream pdfStream,Guid documentId, CancellationToken ct = default);
    
}
