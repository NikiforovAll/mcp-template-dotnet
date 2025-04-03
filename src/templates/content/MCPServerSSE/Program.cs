using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();
app.Run();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
