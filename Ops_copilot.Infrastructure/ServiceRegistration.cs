using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Ops_copilot.Application;
using Ops_copilot.Domain.Interfaces;
using Ops_copilot.Infrastructure.Services;
using Ops_copilot.Infrastructure.Storage;

namespace Ops_copilot.Infrastructure;

/// <summary>
/// Dependency injection registration for infrastructure services.
/// Configures Ollama clients via Semantic Kernel abstractions for easy provider swapping.
/// To switch to GitHub Models, Azure OpenAI, or another provider, replace the AddOllama* calls
/// with the corresponding AddAzure* or AddGitHub* methods while keeping the rest unchanged.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string ollamaEndpoint,
        string chatModel,
        string embeddingModel)
    {
        services.AddLogging();

        // Register in-memory vector storage as singleton to preserve data across requests
        services.AddSingleton<IInMemoryVectorDatabase, InMemoryVectorStorage>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<ISemanticAIService, SemanticAIService>();

        // Register Semantic Kernel with Ollama chat and embedding services
        // This abstraction layer allows swapping to GitHub Models, Azure OpenAI, etc. by changing only this registration
        services.AddKernel()
            .AddOllamaChatCompletion(chatModel, new Uri(ollamaEndpoint))
            .AddOllamaEmbeddingGenerator(embeddingModel, new Uri(ollamaEndpoint));

        return services;
    }
}
