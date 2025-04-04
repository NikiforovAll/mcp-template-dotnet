var builder = DistributedApplication.CreateBuilder(args);

builder.AddMCPInspector().WithStdio<Projects.MCPServer>();

builder.Build().Run();
