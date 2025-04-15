using Microsoft.AspNetCore.SignalR;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.AddSignalR();

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

var app = builder.Build();

app.UseFileServer();

app.MapHub<DrawHub>("/draw");

await app.RunAsync();

[McpServerToolType]
public static class DrawTool
{
    [McpServerTool, Description("Draws a line on a whiteboard.")]
    public static async Task DrawLine(IHubContext<DrawHub> hubContext, int startX, int startY, int endX, int endY, string color)
    {
        await hubContext.Clients.All.SendAsync("draw", startX, startY, endX, endY, color);
    }
}

public class DrawHub : Hub
{
    public Task Draw(int prevX, int prevY, int currentX, int currentY, string color)
    {
        return Clients.Others.SendAsync("draw", prevX, prevY, currentX, currentY, color);
    }
}
