var builder = DistributedApplication.CreateBuilder(args);

// https://github.com/modelcontextprotocol/inspector/issues/239
var mcp = builder.AddProject<Projects.MCPServerSSE>("server");
builder.AddMCPInspector().WithSSE(mcp);

builder.Build().Run();
