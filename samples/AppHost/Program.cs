var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.MCPServer>("server");

builder.AddMCPInspector().WithSSE(mcp);

builder.Build().Run();
