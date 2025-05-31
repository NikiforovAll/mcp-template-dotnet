var builder = DistributedApplication.CreateBuilder(args);

var sse = builder.AddProject<Projects.MCPServerHybrid>("server");
builder.AddMCPInspector("mcp-http", serverPort: 9000, clientPort: 8080).WithMcp(sse);

builder
    .AddMCPInspector("mcp-stdio")
    .WithStdio<Projects.MCPServerHybrid>();

builder.Build().Run();
