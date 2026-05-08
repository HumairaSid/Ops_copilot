Ops Copilot — PRD & Task List
Product Requirements Document · V1 POC
Author: Humaira Siddiqui  |  Target: V1 POC Complete  |  April 2026

1. Overview
Ops Copilot is a local-first RAG (Retrieval-Augmented Generation) system for enterprise operational data. V1 is a focused proof-of-concept demonstrating the core ingestion and AI inference pipeline. It is scoped to a single PDF upload, with two AI-powered features: document summarization and question answering. The system is designed for technical credibility — Clean Architecture, TDD, and a full test pyramid from unit through to Playwright E2E.

2. User Stories
Ingestion
As a user, I can upload a single PDF file so the system processes and stores it for AI queries.
As a user, I receive confirmation of successful upload including the number of chunks generated.
As a user, I receive a clear error if I upload an unsupported file type or an oversized file.

Summarization
As a user, I can request a summary of my uploaded document and receive a concise, readable paragraph.
As a user, the summary reflects the actual content of the document, not hallucinated content.

Question & Answer
As a user, I can ask a natural language question about my uploaded document.
As a user, the answer is grounded in the document content and includes references to the source chunks.
As a user, if my question cannot be answered from the document, the system tells me so rather than hallucinating.

3. Acceptance Criteria
Feature	Acceptance Criterion	Test Type
File upload	PDF ≤ 10MB accepted; non-PDF or oversized rejected with error message	Integration
Chunking	All chunks ≤ 400 tokens; no mid-sentence splits; overlap = 80 tokens	Unit
Embedding	Each chunk has a non-null float[] embedding stored alongside it	Unit + Integration
Summarization	Returns non-empty string within 30 seconds for a 5-page PDF	Integration + E2E
Q&A	Answer is non-empty; response body includes at least 1 source chunk reference	Integration + E2E
API docs	All 4 endpoints visible and testable in Swagger UI	E2E
Test coverage	Unit: ≥ 80% coverage on Application layer; Integration: all endpoints; E2E: happy path flows	All

4. Task List
All tasks are V1 scope. Status reflects current state as of document creation. Statuses: ✅ Done · 🔧 In Progress · 📋 Planned · ⬜ Backlog

EPIC 1 — Foundation & Project Setup
#	Task	Owner	Status
1.1	Create solution with 4 src projects (Domain, Application, Infrastructure, Api)	Humaira	✅ Done
1.2	Add 3 test projects (Unit, Integration, E2E) to solution	Humaira	📋 Planned
1.3	Configure appsettings.json with Ollama endpoint, model name, chunking params	Humaira	✅ Done
1.4	Register all dependencies in DI container (Program.cs) — interfaces → implementations	Humaira	✅ Done
1.5	Add Swashbuckle / Swagger with XML doc comments enabled	Humaira	📋 Planned

EPIC 2 — Domain Layer
#	Task	Owner	Status
2.1	Define Document entity (Id, FileName, UploadedAt, Status)	Humaira	✅ Done
2.2	Define DocumentChunk entity (Id, DocumentId, Text, PageNumber, ChunkIndex, TokenCount)	Humaira	✅ Done
2.3	Define IVectorStore interface (StoreAsync, SearchAsync, GetAllByDocumentIdAsync)	Humaira	✅ Done
2.4	Define IDocumentRepository interface (SaveAsync, GetByIdAsync)	Humaira	📋 Planned

EPIC 3 — Application Layer
#	Task	Owner	Status
3.1	Define ISemanticAIService interface (SummarizeAsync, AnswerAsync)	Humaira	✅ Done
3.2	Define IEmbeddingService interface (GenerateAsync)	Humaira	✅ Done
3.3	Implement ProcessDocumentUseCase (extract → chunk → embed → store)	Humaira	🔧 In Progress
3.4	Implement SummarizeDocumentUseCase (fetch chunks → build prompt → call AI)	Humaira	🔧 In Progress
3.5	Implement QueryDocumentUseCase (embed question → similarity search → call AI with context)	Humaira	🔧 In Progress
3.6	Write SummarizationPrompt template (grounded, no hallucination instruction)	Humaira	✅ Done
3.7	Write QAPrompt template (question + context injection, cite sources instruction)	Humaira	✅ Done

EPIC 4 — Infrastructure Layer
#	Task	Owner	Status
4.1	Implement PdfTextExtractor using PdfPig (page-by-page text extraction)	Humaira	✅ Done
4.2	Implement TokenAwareChunker with SharpToken (paragraph boundary, overlap, max token enforcement)	Humaira	🔧 In Progress
4.3	Implement InMemoryVectorStorage (cosine similarity search, store + retrieve by documentId)	Humaira	✅ Done
4.4	Implement SemanticKernelEmbeddingService (wraps SK ITextEmbeddingGenerationService)	Humaira	✅ Done
4.5	Implement SemanticAIService (wraps Semantic Kernel kernel.InvokeAsync for summarize + Q&A)	Humaira	✅ Done
4.6	Fix Ollama HTTP endpoint handshake (resolve 404 — model endpoint URL format)	Humaira	🔧 In Progress
4.7	Add InMemoryDocumentRepository (implements IDocumentRepository, stores Document metadata)	Humaira	📋 Planned

EPIC 5 — API Layer
#	Task	Owner	Status
5.1	Implement POST /api/document/upload (multipart form, file validation, returns documentId)	Humaira	✅ Done
5.2	Implement POST /api/document/{id}/summarize	Humaira	✅ Done
5.3	Implement POST /api/document/{id}/query	Humaira	✅ Done
5.4	Implement GET /api/document/{id}/status	Humaira	📋 Planned
5.5	Add global exception middleware (ProblemDetails format, no stack traces in prod)	Humaira	📋 Planned
5.6	Add file size and MIME type validation (max 10MB, application/pdf only)	Humaira	📋 Planned
5.7	Add XML doc comments to all controller actions for Swagger	Humaira	📋 Planned

EPIC 6 — Unit Tests (xUnit)
#	Test Case	Class Under Test	Status
6.1	Chunker: single paragraph → 1 chunk	TokenAwareChunker	📋 Planned
6.2	Chunker: long text → multiple chunks, each ≤ ChunkSize tokens	TokenAwareChunker	📋 Planned
6.3	Chunker: no mid-sentence splits across any chunk boundary	TokenAwareChunker	📋 Planned
6.4	Chunker: overlap tokens appear in consecutive chunks	TokenAwareChunker	📋 Planned
6.5	VectorStore: stored chunk retrieved by SearchAsync with high similarity score	InMemoryVectorStorage	📋 Planned
6.6	VectorStore: SearchAsync topK=3 returns exactly 3 results	InMemoryVectorStorage	📋 Planned
6.7	ProcessDocumentUseCase: IEmbeddingService called once per chunk (Moq verify)	ProcessDocumentUseCase	📋 Planned
6.8	SummarizeUseCase: ISemanticAIService.SummarizeAsync called with all chunk texts	SummarizeDocumentUseCase	📋 Planned
6.9	QueryUseCase: IEmbeddingService.GenerateAsync called with question text	QueryDocumentUseCase	📋 Planned
6.10	QueryUseCase: response includes source chunk references from vector store results	QueryDocumentUseCase	📋 Planned
6.11	SummarizationPrompt: rendered string contains injected chunk content	SummarizationPrompt	📋 Planned
6.12	QAPrompt: rendered string contains both question and context chunks	QAPrompt	📋 Planned

EPIC 7 — Integration Tests (xUnit + WebApplicationFactory)
#	Test Case	Endpoint	Status
7.1	Valid PDF upload returns 200 with documentId and chunkCount	POST /upload	📋 Planned
7.2	Non-PDF file upload returns 400 Bad Request	POST /upload	📋 Planned
7.3	PDF > 10MB returns 413 Payload Too Large	POST /upload	📋 Planned
7.4	Summarize with valid documentId returns 200 and non-empty summary (mocked AI)	POST /summarize	📋 Planned
7.5	Summarize with unknown documentId returns 404	POST /summarize	📋 Planned
7.6	Query with valid question returns 200 with answer and sources array (mocked AI)	POST /query	📋 Planned
7.7	Query with empty question string returns 400	POST /query	📋 Planned
7.8	Full pipeline: upload → summarize → query completes without exception	All	📋 Planned

EPIC 8 — E2E Tests (Microsoft Playwright .NET)
#	Scenario	Status
8.1	Swagger UI loads; all 4 endpoints listed with descriptions	📋 Planned
8.2	Upload a test PDF via Swagger UI → response contains documentId (non-empty GUID)	📋 Planned
8.3	POST /summarize with returned documentId → summary field is non-empty string	📋 Planned
8.4	POST /query with factual question → answer is non-empty, sources array has ≥ 1 entry	📋 Planned
8.5	POST /query with off-topic question → answer does not assert facts not in document	📋 Planned
8.6	Upload invalid file type (.txt) → 400 error visible in Swagger response panel	📋 Planned
8.7	Upload second PDF → query on first document ID still returns correct first document answers	📋 Planned

5. Definition of Done — V1 POC
•	All EPIC 1–5 tasks at status ✅ Done
•	All 12 unit test cases passing with ≥ 80% Application layer coverage
•	All 8 integration test cases passing (Ollama mocked)
•	All 7 Playwright E2E scenarios passing against running API with real Ollama
•	README updated to reflect running status (remove 'in progress' Ollama language)
•	GitHub commit history shows incremental, well-described commits across all epics
•	Swagger UI accessible at /swagger with all endpoints documented
