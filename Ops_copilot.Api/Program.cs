using Ops_copilot.Infrastructure;
using Ops_copilot.Application;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---
var ollamaSection = builder.Configuration.GetSection(OllamaSettings.SectionName);
var ollamaSettings = ollamaSection.Get<OllamaSettings>() ?? new OllamaSettings();
builder.Services.Configure<OllamaSettings>(ollamaSection);

var ollamaUrl = string.IsNullOrWhiteSpace(ollamaSettings.Endpoint)
    ? builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434"
    : ollamaSettings.Endpoint;

var chatModel = !string.IsNullOrWhiteSpace(ollamaSettings.ChatModel)
    ? ollamaSettings.ChatModel
    : (builder.Configuration["Ollama:ModelId"] ?? "llama3:latest");

var embeddingModel = !string.IsNullOrWhiteSpace(ollamaSettings.EmbeddingModel)
    ? ollamaSettings.EmbeddingModel
    : "nomic-embed-text:latest"; // Default Ollama embedding model

// --- 2. SERVICE REGISTRATION ---
builder.Services.AddLogging();

// Resolve all AI, PDF, and in-memory stores
builder.Services.AddInfrastructure(ollamaUrl, chatModel, embeddingModel);

// Add controllers for API
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Log resolved Ollama configuration
var logger = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("Startup");
logger.LogInformation("Ollama Endpoint: {Endpoint}", ollamaUrl);
logger.LogInformation("Ollama Model: {Model}", chatModel);

// --- 3. MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ops Copilot API V1");
        c.RoutePrefix = "swagger"; // Swagger UI will be at http://localhost:5006/swagger
    });
}

// Use routing and authorization
app.UseRouting();
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

// Optional: serve static files (Swagger uses this)
//app.UseStaticFiles();

app.Run();
