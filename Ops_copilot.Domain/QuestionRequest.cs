using System;

namespace Ops_copilot.Domain;

public record QuestionRequest(
    Guid DocumentId, 
    string Question, 
    bool UseContextOnly = true);