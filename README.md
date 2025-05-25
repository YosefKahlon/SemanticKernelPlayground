# Semantic Kernel Chat Application

A .NET 9 console application that implements a chat interface using Microsoft's Semantic Kernel and Azure OpenAI services.

## Features

- Console-based chat interface with Azure OpenAI
- Real-time streaming of AI responses
- Conversation history management
- Git repository integration (view commits, manage versions, commit and push changes)
- Generate professional release notes from commit history using prompt plugins

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

**Note:** For best results, ensure your working directory is set to the project root when running the application.