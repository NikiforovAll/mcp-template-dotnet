var builder = DistributedApplication.CreateBuilder(args);

var mcp = builder.AddProject<Projects.MCPServerRemote>("server");
builder.AddMCPInspector().WithMcp(mcp);

builder.Build().Run();
