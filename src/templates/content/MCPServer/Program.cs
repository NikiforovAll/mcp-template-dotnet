using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(cfg =>
{
    cfg.LogToStandardErrorThreshold = LogLevel.Trace;
    cfg.FormatterName = "json";
});

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
builder.Services.AddTransient<EchoTool>();

await builder.Build().RunAsync();

[McpServerToolType]
public class EchoTool(ILogger<EchoTool> logger)
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public string Echo(string message)
    {
        logger.LogTrace("Echo called with message: {Message}", message);
        logger.LogDebug("Echo called with message: {Message}", message);
        logger.LogInformation("Echo called with message: {Message}", message);
        logger.LogWarning("Echo called with message: {Message}", message);
        logger.LogError("Echo called with message: {Message}", message);
        logger.LogCritical("Echo called with message: {Message}", message);

        return $"hello {message}";
    }
}
