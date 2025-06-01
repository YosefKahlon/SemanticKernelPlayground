# Semantic Kernel Chat Application

A .NET 9 console application that implements a chat interface using Microsoft's Semantic Kernel and Azure OpenAI services.

## Features

- Console-based chat interface with Azure OpenAI
- Real-time streaming of AI responses
- Conversation history management
- Git repository integration (view commits, manage versions, commit and push changes)
- Generate professional release notes from commit history using prompt plugins
- **Retrieval-Augmented Generation (RAG) support for code/documentation Q&A**

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Azure OpenAI resource and API key

## Setup

 Configure Azure OpenAI credentials in `appsettings.Development.json`:
   ```json
   {
     "ModelName": "your-model-name",
     "Endpoint": "https://your-azure-openai-endpoint/",
     "ApiKey": "your-azure-openai-api-key"
   }
   ```

## Usage

- On launch, the app connects to Azure OpenAI and waits for your input.
- Type your message and press Enter to chat with the AI.
- Type `exit` to quit the application.

### Git Plugin Commands

You can use the following commands in the chat to interact with Git repositories:

- `SetRepositoryPath <path>`: Set the path to your local Git repository.
- `GetLatestCommits <N>`: List the latest N commits from the set repository.
- `GetLatestCommitsFromGitHubAsync <repo-url> <N>`: List the latest N commits from a GitHub repository.
- `PatchSemVer <major|minor|patch>`: Bump the semantic version and store it.
- `GetLatestStoredVersion`: Show the latest stored release version.
- `CommitChanges <message>`: Commit all changes with a message.
- `PushWithToken`: Push committed changes to the remote (you will be prompted for credentials).

### Release Notes Generation

- The app uses prompt plugins (e.g., `GenerateReleaseNotes.skprompt.txt`) to generate professional release notes from commit history.

---

## Retrieval-Augmented Generation (RAG)

This application now supports RAG, enabling the AI to answer questions grounded in your codebase and documentation:

- **Vector Store**: Documentation and code are split into chunks (using `CodeChunker`), embedded as vectors (using `TextEmbeddingGenerator`), and stored in a vector collection for fast semantic search.
- **Semantic Search**: When you ask a question, the app retrieves relevant documentation/code chunks from the vector store using semantic similarity.
- **Cited Answers**: The AI generates answers based on retrieved chunks and cites sources (file and chunk index) for transparency.

### Key Components

- **Vector Store**: Stores vector embeddings of documentation/code chunks for efficient similarity search.
- **TextEmbeddingGenerator**: Converts text chunks into vector embeddings using Azure OpenAI.
- **Collection**: The logical grouping of all vectorized documentation/code chunks.
- **SemanticKernel**: Orchestrates the AI, plugins, and RAG workflow.
- **DocumentationPlugin**: Handles ingestion, chunking, embedding, and retrieval of documentation/code for RAG.
- **GitPlugin**: Provides Git integration features (unrelated to RAG, but works alongside).
- **PromptPlugins**: Contains prompt templates for tasks like release notes generation.
- **DocumentationChunk**: Represents a chunk of documentation/code, with metadata (file, index, content).
- **TextSearchResult**: (If used) Represents the result of a semantic search, including the matched chunk and similarity score.
- **CodeChunker**: Splits code files into logical chunks (by class/method) for
