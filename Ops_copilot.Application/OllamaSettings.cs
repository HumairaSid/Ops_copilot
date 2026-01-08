
namespace Ops_copilot.Application;

public record OllamaSettings
{
    public const string SectionName = "Ollama";
    public string Endpoint { get; init; } = string.Empty;
    public string ChatModel { get; init; } = string.Empty;
    public string EmbeddingModel { get; init; } =  string.Empty;
}
