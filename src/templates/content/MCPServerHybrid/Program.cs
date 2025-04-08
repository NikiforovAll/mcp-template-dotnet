using MCPServerHybrid;

var builder = WebApplication.CreateBuilder(args);
builder.WithMcpServer(args).WithToolsFromAssembly();

var app = builder.Build();
app.MapMcpServer(args);
app.Run();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
