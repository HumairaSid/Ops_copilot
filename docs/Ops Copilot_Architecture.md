Ops Copilot
Architecture Design Document — V1 (POC)
RAG System · Single File Upload · Summarization & Q&A
Author: Humaira Siddiqui  |  Status: In Progress  |  April 2026

1. Purpose & Scope
This document describes the architecture of Ops Copilot V1 — a proof-of-concept RAG (Retrieval-Augmented Generation) pipeline. The system accepts a single uploaded file (PDF), processes it into token-aware chunks, generates embeddings, stores them in an in-memory vector database, and exposes two AI-powered features: document summarization and a question-and-answer interface.
This is a local-first, privacy-preserving design using Ollama as the LLM provider and Microsoft Semantic Kernel as the AI orchestration layer. No data leaves the host machine.

2. Design Goals
Functional Goals (V1)
•	Accept a single PDF file upload via REST API
•	Extract and chunk text using token-aware splitting (no mid-sentence splits, no mid-paragraph splits where avoidable)
•	Generate embeddings for each chunk using Semantic Kernel's embedding service
•	Store chunks and their embeddings in an in-memory vector store (interface-first, swappable)
•	Expose a /summarize endpoint that produces a concise document summary via LLM
•	Expose a /query endpoint that retrieves relevant chunks and generates a grounded answer

Non-Functional Goals (V1)
•	Clean Architecture — Domain layer has zero dependencies on Infrastructure
•	All AI and storage dependencies accessed through interfaces (DI-friendly, testable)
•	xUnit unit and integration test coverage on Application and Infrastructure layers
•	Playwright E2E tests covering the full upload → summarize → query flow
•	Swagger/OpenAPI documentation on all endpoints

3. System Architecture
3.1 Layer Overview
The solution follows Clean Architecture with strict unidirectional dependencies. All layers depend inward — Infrastructure knows about Application, Application knows about Domain, Domain knows nothing external.

Layer	Project	Responsibility
Domain	Ops_copilot.Domain	Entities (DocumentChunk, Document), repository interfaces (IVectorStore, IDocumentRepository), no external dependencies
Application	Ops_copilot.Application	Use case interfaces (IDocumentProcessor, ISemanticAIService), prompt templates, orchestration logic, DTOs
Infrastructure	Ops_copilot.Infrastructure	PDF extraction (PdfPig), tokenizer, chunker, Semantic Kernel wiring, Ollama connector, InMemoryVectorStorage implementation
API	Ops_copilot.Api	ASP.NET Core controllers, DI composition root, middleware, Swagger config, file upload handling
Tests	*.Tests (3 projects)	Unit (xUnit), Integration (xUnit + WebApplicationFactory), E2E (Playwright)

3.2 Solution Folder Structure
Ops_copilot/
├── src/
│   ├── Ops_copilot.Domain/
│   │   ├── Entities/
│   │   │   ├── Document.cs
│   │   │   └── DocumentChunk.cs
│   │   └── Interfaces/
│   │       ├── IVectorStore.cs
│   │       └── IDocumentRepository.cs
│   ├── Ops_copilot.Application/
│   │   ├── Interfaces/
│   │   │   ├── IDocumentProcessor.cs
│   │   │   ├── ISemanticAIService.cs
│   │   │   └── IEmbeddingService.cs
│   │   ├── UseCases/
│   │   │   ├── ProcessDocumentUseCase.cs
│   │   │   ├── SummarizeDocumentUseCase.cs
│   │   │   └── QueryDocumentUseCase.cs
│   │   └── Prompts/
│   │       ├── SummarizationPrompt.cs
│   │       └── QAPrompt.cs
│   ├── Ops_copilot.Infrastructure/
│   │   ├── Parsing/
│   │   │   └── PdfTextExtractor.cs
│   │   ├── Chunking/
│   │   │   ├── TokenAwareChunker.cs
│   │   │   └── ChunkingOptions.cs
│   │   ├── Embeddings/
│   │   │   └── SemanticKernelEmbeddingService.cs
│   │   ├── VectorStore/
│   │   │   └── InMemoryVectorStorage.cs
│   │   └── AI/
│   │       └── SemanticAIService.cs
│   └── Ops_copilot.Api/
│       ├── Controllers/
│       │   └── DocumentController.cs
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    ├── Ops_copilot.Unit.Tests/
    ├── Ops_copilot.Integration.Tests/
    └── Ops_copilot.E2E.Tests/

3.3 RAG Pipeline — Data Flow
The pipeline has two phases: Ingestion (run once on upload) and Retrieval (run on each query or summarization request).

Phase 1 — Ingestion
1.	HTTP POST /api/document/upload receives multipart form data (PDF file)
2.	DocumentController validates file type and size, passes stream to ProcessDocumentUseCase
3.	PdfTextExtractor (Infrastructure) extracts raw text page-by-page using PdfPig
4.	TokenAwareChunker splits text into overlapping token windows (configurable: chunk size, overlap, max tokens)
5.	SemanticKernelEmbeddingService generates a float[] embedding vector per chunk using Semantic Kernel's ITextEmbeddingGenerationService
6.	InMemoryVectorStorage stores each DocumentChunk with its embedding and metadata (page number, source filename, chunk index)
7.	API returns document ID and chunk count

Phase 2A — Summarization
8.	HTTP POST /api/document/{id}/summarize
9.	SummarizeDocumentUseCase retrieves all chunks for the document from InMemoryVectorStorage
10.	Chunks concatenated (with token budget awareness) and injected into SummarizationPrompt template
11.	SemanticAIService sends prompt to Ollama via Semantic Kernel kernel.InvokeAsync()
12.	Summary text returned in response body

Phase 2B — Question & Answer
13.	HTTP POST /api/document/{id}/query with body { "question": "..." }
14.	QueryDocumentUseCase embeds the question text using IEmbeddingService
15.	Cosine similarity search over InMemoryVectorStorage returns top-K relevant chunks
16.	Chunks + question injected into QAPrompt template with grounding instruction
17.	SemanticAIService sends prompt to Ollama, receives grounded answer
18.	Answer + source chunk references returned in response

3.4 Token-Aware Chunking Strategy
Chunking is the most critical step for retrieval quality. The TokenAwareChunker in V1 implements the following strategy:

Parameter	V1 Default	Rationale
ChunkSize (tokens)	400	Fits within Ollama context window with room for prompt + answer
ChunkOverlap (tokens)	80	Ensures context continuity across chunk boundaries
Split boundary	Paragraph > sentence	Never splits mid-sentence; prefers natural paragraph breaks
Tokenizer	SharpToken (cl100k_base)	Matches OpenAI/Ollama token counting behaviour accurately
Metadata attached	page, chunkIndex, sourceFile	Enables source citation in answers

3.5 Key Interfaces (Domain / Application)

IVectorStore (Domain)
public interface IVectorStore
{
    Task StoreAsync(DocumentChunk chunk, float[] embedding);
    Task<IEnumerable<ScoredChunk>> SearchAsync(float[] queryEmbedding, int topK = 5);
    Task<IEnumerable<DocumentChunk>> GetAllByDocumentIdAsync(Guid documentId);
}

ISemanticAIService (Application)
public interface ISemanticAIService
{
    Task<string> SummarizeAsync(IEnumerable<string> chunks);
    Task<string> AnswerAsync(string question, IEnumerable<string> contextChunks);
}

IEmbeddingService (Application)
public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(string text);
}

3.6 Technology Stack
Concern	Technology	Notes
Runtime	.NET 8 / C#	LTS, aligns with current sabbatical work
Web framework	ASP.NET Core Minimal API	Lightweight, Swagger-compatible
AI orchestration	Microsoft Semantic Kernel	Provider-agnostic; swap Ollama → Azure OpenAI via config
Local LLM	Ollama (llama3 / mistral)	Privacy-first, no external API calls required
PDF parsing	PdfPig	MIT-licensed, no native deps, works in containers
Tokenizer	SharpToken	Accurate token counting for cl100k_base vocab
Vector storage	InMemoryVectorStorage (custom)	Interface-first; swap to Qdrant / Azure AI Search in V2
Unit testing	xUnit + Moq	Standard .NET testing stack
Integration testing	xUnit + WebApplicationFactory	Spins up real API in-process
E2E testing	Microsoft Playwright (.NET)	Tests full browser → API → LLM flow
API docs	Swagger / Swashbuckle	Auto-generated from controller XML docs
Containerisation	Docker	Single Dockerfile, planned Compose for Ollama + API

3.7 API Contracts
Method	Endpoint	Request	Response
POST	/api/document/upload	multipart/form-data: file (PDF)	{ documentId, chunkCount, fileName }
POST	/api/document/{id}/summarize	–	{ summary, tokenCount, documentId }
POST	/api/document/{id}/query	{ question: string }	{ answer, sources: [{ chunkIndex, page, excerpt }] }
GET	/api/document/{id}/status	–	{ documentId, status, chunkCount, fileName }

4. Testing Strategy
4.1 Test Pyramid
Three test projects mirror the architecture layers. Each has a clear scope — unit tests are fast and isolated; integration tests validate wiring; E2E tests validate the full user journey.

Layer	Framework	What Is Tested	What Is Mocked
Unit	xUnit + Moq	Use cases, chunker logic, prompt building, similarity search	IVectorStore, ISemanticAIService, IEmbeddingService
Integration	xUnit + WebApplicationFactory	Full HTTP → controller → use case → infrastructure pipeline	Ollama only (via mock ISemanticAIService or stub HTTP)
E2E	Playwright (.NET)	Upload PDF → summarize → query via real browser/HTTP	Nothing — real Ollama required

4.2 Key Unit Test Cases
•	TokenAwareChunker: single-paragraph document produces 1 chunk
•	TokenAwareChunker: document exceeding chunk size produces multiple chunks with correct overlap
•	TokenAwareChunker: never splits mid-sentence (sentence boundary respected)
•	TokenAwareChunker: chunk token count never exceeds ChunkSize limit
•	InMemoryVectorStorage: StoreAsync followed by SearchAsync returns correct chunk by cosine similarity
•	InMemoryVectorStorage: SearchAsync with topK=3 returns exactly 3 results
•	ProcessDocumentUseCase: calls IEmbeddingService once per chunk
•	ProcessDocumentUseCase: calls IVectorStore.StoreAsync once per chunk
•	SummarizeDocumentUseCase: calls ISemanticAIService.SummarizeAsync with all chunk texts
•	QueryDocumentUseCase: calls IEmbeddingService.GenerateAsync for the question
•	QueryDocumentUseCase: calls IVectorStore.SearchAsync with question embedding
•	QueryDocumentUseCase: response includes source chunk references
•	SummarizationPrompt: rendered prompt contains injected chunk content
•	QAPrompt: rendered prompt contains both question and context

4.3 Key Integration Test Cases
•	POST /api/document/upload with valid PDF returns 200 and documentId
•	POST /api/document/upload with non-PDF file returns 400
•	POST /api/document/upload with oversized file returns 413
•	POST /api/document/{id}/summarize with valid id returns 200 and non-empty summary
•	POST /api/document/{id}/query with valid question returns 200 with answer and sources
•	POST /api/document/{id}/query with unknown documentId returns 404
•	GET /api/document/{id}/status returns correct chunkCount after upload
•	Full pipeline: upload → summarize → query completes without error (mocked Ollama)

4.4 Key Playwright E2E Tests
•	Navigate to Swagger UI — all endpoints visible and documented
•	Upload a sample PDF via /upload endpoint in Swagger UI — response contains documentId
•	POST /summarize with returned documentId — response contains summary text
•	POST /query with a factual question about the uploaded document — answer is non-empty
•	POST /query with an off-topic question — answer indicates the topic is not in the document
•	Upload a second PDF — verify first document data is not contaminated
•	Upload an invalid file type — verify 400 response displayed in UI

5. V2 Roadmap (Out of Scope for V1)
•	Swap InMemoryVectorStorage for Qdrant (via IVectorStore — zero application code change)
•	Add Azure OpenAI as an alternative LLM provider (configuration toggle)
•	Multi-file support with per-document namespacing in vector store
•	GitHub Actions CI pipeline: build → unit tests → integration tests on every push
•	Docker Compose: Ollama container + API container orchestrated together
•	Streaming responses for summarize and query (SSE or chunked transfer)
•	Frontend (React/TypeScript) — file drag-drop, chat-style Q&A interface

6. Risks & Mitigations
Risk	Impact	Mitigation
Ollama endpoint instability (current 404)	Summarize/query endpoints non-functional	Implement mock ISemanticAIService for all tests; add Azure OpenAI path in V2
In-memory store lost on restart	No persistence between sessions	Documented as POC constraint; IVectorStore interface ready for persistent store swap
Token budget overflow on large PDFs	LLM context window exceeded	Token-aware chunker enforces hard max; summarization uses chunk sampling if total exceeds budget
Ollama model quality variation	Poor answer quality depending on model	Model name is configurable in appsettings; tested with llama3 and mistral

