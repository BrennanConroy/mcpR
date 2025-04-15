using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.ClientModel;

var clientTransport = new StdioClientTransport(new()
{
    Name = "SignalR Server",
    Command = "dotnet",
    Arguments = [ "run", "--project", "../../../../server/server.csproj"],
});

await using var client = await McpClientFactory.CreateAsync(clientTransport);

// Print the list of tools available from the server.
var tools = await client.ListToolsAsync();
Console.WriteLine("Available tools:");
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}
Console.WriteLine();

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

using IChatClient chatClient =
    new AzureOpenAIClient(new Uri(config["OPENAI_ENDPOINT"]!),
        new ApiKeyCredential(config["OPENAI_API_KEY"]!)).AsChatClient("gpt-4.1")
    .AsBuilder().UseFunctionInvocation().Build();

// Have a conversation, making all tools available to the LLM.
List<ChatMessage> messages = [];
messages.Add(new ChatMessage(ChatRole.System, "You are an artist that can draw very detailed drawings," +
    " don't be afraid to use many lines per object to draw more accurately."));

while (true)
{
    Console.Write("Q: ");
    messages.Add(new(ChatRole.User, Console.ReadLine()));

    List<ChatResponseUpdate> updates = [];
    await foreach (var update in chatClient.GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update);
        updates.Add(update);
    }
    Console.WriteLine();

    messages.AddMessages(updates);
}
