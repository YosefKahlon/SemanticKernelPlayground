using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Model;
using SemanticKernelPlayground.Plugins;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");
var embeddingModel = configuration["EmbeddingModel"] ?? throw new ApplicationException("EmbeddingModel not found");

// Add embedding and vector store to kernel
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embeddingModel, endpoint, apiKey)
    .AddInMemoryVectorStore();

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Information));

builder.Plugins.AddFromType<GitPlugin>();
builder.Plugins.AddFromType<DocumentationPlugin>();
builder.Plugins.AddFromPromptDirectory("Plugins/PromptPlugins");

var kernel = builder.Build();

// [DEBUG] Generate documentation chunks for the codebase
var logger = kernel.GetRequiredService<ILogger<DocumentationPlugin>>();
var docPlugin = new DocumentationPlugin();
var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));
var docs = docPlugin.GenerateDocumentation(projectRoot);
Console.WriteLine($"[DEBUG] Generated {docs.Count} documentation chunks.");

// [DEBUG] Ingest documentation into vector store
var vectorStore = kernel.GetRequiredService<IVectorStore>();
var textEmbeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
var collectionName = "CodebaseDocs";
var collection = vectorStore.GetCollection<string, DocumentationChunk>(collectionName);
await collection.CreateCollectionIfNotExistsAsync();
foreach (var doc in docs)
{
    var embedding = await textEmbeddingGenerator.GenerateEmbeddingAsync(doc.Content);
    var docWithEmbedding = doc with { Embedding = embedding };
    await collection.UpsertAsync(docWithEmbedding, CancellationToken.None);
}
Console.WriteLine($"[DEBUG] Ingested documentation into vector store.");

// [DEBUG] Set up text search plugin
var textSearch = new VectorStoreTextSearch<DocumentationChunk>(collection, textEmbeddingGenerator);
var searchPlugin = textSearch.CreateWithGetSearchResults("CodebaseSearchPlugin");
kernel.Plugins.Add(searchPlugin);

// [DEBUG] Configure chat to use search plugin
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
AzureOpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
var history = new ChatHistory();
history.AddSystemMessage("You are a RAG-enabled assistant. For every query:\n" +
                         "1. Always invoke the 'CodebaseSearchPlugin' to retrieve relevant documentation chunks.\n" +
                         "2. Base your answer on those chunks whenever possible.\n" +
                         "3. Cite each fact with its source in the form (FileName, chunk #).\n" +
                         "Keep answers concise and grounded in the retrieved material.");

do
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Me > ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (userInput == "exit")
    {
        break;
    }

    history.AddUserMessage(userInput!);

    var streamingResponse =
        chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            openAiPromptExecutionSettings,
            kernel);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent > ");
    Console.ResetColor();

    var fullResponse = "";
    await foreach (var chunk in streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(chunk.Content);
        Console.ResetColor();
        fullResponse += chunk.Content;
    }
    Console.WriteLine();

    history.AddMessage(AuthorRole.Assistant, fullResponse);

} while (true);
