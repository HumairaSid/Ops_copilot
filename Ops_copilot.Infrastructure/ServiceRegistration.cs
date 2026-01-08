using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Embeddings;
using OllamaSharp;
using Ops_copilot.Application;
using Ops_copilot.Domain.Interfaces;
using Ops_copilot.Infrastructure.Services;
using Ops_copilot.Infrastructure.Storage;
using OllamaSharp.Models;

namespace Ops_copilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string ollamaEndpoint,
        string modelId)
    {
        services.AddLogging();

        var builder = Kernel.CreateBuilder();

        // Ollama chat completion
        builder.AddOllamaChatCompletion(modelId, new Uri(ollamaEndpoint));

        // Optional: if you want embeddings via Ollama client separately
        builder.AddOllamaEmbeddingGenerator(modelId, new Uri(ollamaEndpoint));

        var kernel = builder.Build();
        services.AddSingleton(kernel);
        // Ollama API Client
        services.AddSingleton(_ =>
            new OllamaApiClient(new Uri(ollamaEndpoint)));

#pragma warning disable CS0618
        services.AddSingleton<IEmbeddingGenerationService<string, float>>(sp =>
   {
       var client = sp.GetRequiredService<OllamaApiClient>();
       return client.AsEmbeddingGenerationService();
   });
#pragma warning restore CS0618

#pragma warning disable SKEXP0050,
        services.AddSingleton<IMemoryStore, VolatileMemoryStore>();

        services.AddSingleton<ISemanticTextMemory>(sp =>
        {
            var generator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var store = sp.GetRequiredService<IMemoryStore>();
            return new SemanticTextMemory(store, generator);
        });

        services.AddSingleton<IInMemoryVectorDatabase, InMemoryVectorStore>();
        services.AddSingleton<IInMemoryDocumentStore, InMemoryDocumentStore>();
        services.AddScoped<IPdfService, PdfService>();

        services.AddScoped<ISemanticAIService, SemanticAIService>();

        return services;
    }
}
