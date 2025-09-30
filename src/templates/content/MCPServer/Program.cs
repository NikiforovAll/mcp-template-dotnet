using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(cfg => cfg.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
builder.Services.AddTransient<EchoTool>();

await builder.Build().RunAsync();

[McpServerToolType]
public class EchoTool(ILogger<EchoTool> logger)
{
    [
        McpServerTool(
            Name = "echo_hello",
            Title = "Writes a hello echo message",
            UseStructuredContent = false
        ),
        Description("Echoes the message back to the client.")
    ]
    public string Echo(string message)
    {
        logger.LogInformation("Echo called with message: {Message}", message);

        return $"hello {message}";
    }
}
