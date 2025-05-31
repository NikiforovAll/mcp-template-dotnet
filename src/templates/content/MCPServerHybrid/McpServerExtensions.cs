namespace MCPServerHybrid;

public static class McpServerExtensions
{
    public static IMcpServerBuilder WithMcpServer(this WebApplicationBuilder builder, string[] args)
    {
        var isStdio = args.Contains("--stdio");

        if (isStdio)
        {
            builder.WebHost.UseUrls("http://*:0"); // random port

            // logs from stderr are shown in the inspector
            builder.Services.AddLogging(builder =>
                builder
                    .AddConsole(consoleBuilder =>
                    {
                        consoleBuilder.LogToStandardErrorThreshold = LogLevel.Trace;
                        consoleBuilder.FormatterName = "json";
                    })
                    .AddFilter(null, LogLevel.Warning)
            );
        }

        var mcpBuilder = isStdio
            ? builder.Services.AddMcpServer().WithStdioServerTransport()
            : builder.Services.AddMcpServer().WithHttpTransport();

        return mcpBuilder;
    }

    public static WebApplication MapMcpServer(this WebApplication app, string[] args)
    {
        var isSse = !args.Contains("--stdio");

        if (isSse)
        {
            app.MapMcp();
        }

        return app;
    }
}
