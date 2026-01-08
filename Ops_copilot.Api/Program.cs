using Ops_copilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---
var ollamaUrl = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
var modelName = builder.Configuration["Ollama:ModelId"] ?? "llama3.1";

// --- 2. SERVICE REGISTRATION ---
builder.Services.AddLogging();

// Resolve all AI, PDF, and in-memory stores
builder.Services.AddInfrastructure(ollamaUrl, modelName);

// Add controllers for API
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
app.UseStaticFiles();

app.Run();
