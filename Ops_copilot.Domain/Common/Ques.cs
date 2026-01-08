using System;

namespace Ops_copilot.Domain.Common;

 public class AskQuestionRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class AnswerResponse
    {
        public string Answer { get; set; } = string.Empty;
    }