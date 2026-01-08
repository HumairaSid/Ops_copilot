# **Generative AI RAG Prototype (.NET 8 + Semantic Kernel)**

*A Clean Architecture implementation of a Retrieval-Augmented Generation (RAG) system using Semantic Kernel, Ollama (Local LLM), and In-Memory Vector Storage.*

---

## 🚀 **Overview**
This POC demonstrates the integration of **Local LLMs** with enterprise-grade **.NET 8 architecture**. It provides a fully functional RAG pipeline to bridge the gap between unstructured data and AI-driven insights.

**Key Features:**
- **Document Ingestion:** PDF processing and text extraction.
- **Vectorization:** Chunking and embedding generation via Semantic Kernel.
- **RAG Implementation:** Semantic search with local LLM orchestration.
- **Local-First:** Optimized for privacy and cost using Ollama.

---

## 🏗️ **Architecture & Project Structure**
Built with **Clean Architecture principles** to ensure high maintainability and clear separation of concerns.

### **Logic Breakdown**
| Layer | Responsibility | Key Logic Files |
| :--- | :--- | :--- |
| **API** | Entry point & Controllers | `DocumentController.cs`, `Program.cs` |
| **Application** | AI Orchestration & Interfaces | `ISemanticAIService.cs`, `PromptTemplates.cs` |
| **Domain** | Core Entities & Models | `DocumentChunk.cs`, `VectorDataAttributes.cs` |
| **Infrastructure** | PDF Parsing & Vector Storage | `SemanticAIService.cs`, `InMemoryVectorStorage.cs` |

---

## 📂 **Folder Structure**
```text
Ops_copilot
├── Ops_copilot.Api
│   ├── DocumentController.cs
│   └── Program.cs
├── Ops_copilot.Application
│   ├── ISemanticAIService.cs
│   ├── OllamaSettings.cs
│   └── PromptTemplates.cs
├── Ops_copilot.Domain
│   ├── DocumentChunkRecord.cs
│   ├── Common
│   │   ├── Document.cs
│   │   └── DocumentChunk.cs
│   ├── Interfaces
│   │   ├── IInMemoryVectorDatabase.cs
│   │   └── IPdfService.cs
│   └── VectorDataAttributes.cs
└── Ops_copilot.Infrastructure
    ├── Services
    │   ├── PdfService.cs
    │   └── SemanticAIService.cs
    ├── Storage
    │   ├── InMemoryDocumentStore.cs
    │   └── InMemoryVectorStorage.cs
    └── ServiceRegistration.cs
