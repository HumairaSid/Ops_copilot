namespace Ops_copilot.Application;

public static class PromptTemplates
{
    public const string SummarizeTemplate = @"
        Summarize the following text concisely.
        Target Language: {{$targetLanguage}}
        Max Length: {{$maxLength}} words.
        
        Text:
        {{$input}}";

    public const string RagAskTemplate = @"
        Answer the question using ONLY the provided context. 
        If the answer is not contained within the context, respond with 'I do not have enough information to answer this.'

        Context:
        {{$context}}

        Question: {{$question}}
        
        Answer:";
}