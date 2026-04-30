# Ops_copilot — CLAUDE.md

## Commands

```bash
# Build
dotnet build Ops_copilot.sln

# Run API (requires local Ollama on :11434)
dotnet run --project Ops_copilot.Api/Ops_copilot.Api.csproj

# All tests
dotnet test Ops_copilot.sln

# Filter by class
dotnet test --filter "FullyQualifiedName~PdfServiceTests"

# Unit tests only
dotnet test --filter "Category=Unit"

# Coverage (requires Coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture (Clean Architecture)

```
Domain          → core models, interfaces, Result<T> — no dependencies
Application     → AI orchestration, ISemanticAIService — depends on Domain only
Infrastructure  → PdfPig, Semantic Kernel + Ollama, in-memory stores
Api             → ASP.NET Core 8, DocumentController
```

## Key Patterns

- **Result<T>** — all service responses; no raw exceptions bubble up
- **Ingestion flow** — PDF → PdfPig extraction → token-aware chunking → SK embedding → in-memory vector store
- **SK warnings** — SKEXP0001 etc. suppressed in Infrastructure.csproj (expected, experimental APIs)

## Ollama Config (`appsettings.json`)

```json
"Ollama": {
  "Endpoint": "http://localhost:11434",
  "ChatModel": "<model-name>",
  "EmbeddingModel": "<model-name>"
}
```

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/documents/upload` | Multipart PDF upload + ingestion |
| GET | `/api/documents/{id}/summary` | AI summary generation |
| POST | `/api/documents/{id}/ask` | RAG-based Q&A |

## Status

| Area | Status |
|------|--------|
| Domain layer | ✅ Done |
| Interfaces | ✅ Done |
| Token-based chunking | ❌ Not implemented |
| SemanticAIService wiring | 🔄 In progress |
| Unit tests | ❌ Not started |
| Integration tests | ❌ Not started |
| E2E (Playwright) | ❌ Not started |
