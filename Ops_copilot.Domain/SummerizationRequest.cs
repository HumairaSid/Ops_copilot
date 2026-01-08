using System;

namespace Ops_copilot.Domain;

public record SummarizationRequest(
    Guid DocumentId, 
    string TargetLanguage = "English", 
    int MaxLength = 500);