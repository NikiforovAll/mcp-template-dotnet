# MCPServer

## Overview
This is a Model Context Protocol (MCP) server implementation built with .NET 9.0. The MCP server provides a communication protocol for facilitating interactions between various components in a model-driven system. This implementation demonstrates how to set up a basic MCP server with custom tools and services.

## Run Locally

Build the project: 

```bash
dotnet build -o Artefacts -c Release
```

Run the inspector:

```bash
npx @modelcontextprotocol/inspector -e DOTNET_ENVIRONMENT=Production dotnet "$(PWD)/Artefacts/MCPServer.dll"
```

## Configure MCP Client

Configure in VS Code with GitHub Copilot, Claude Desktop, or other MCP clients:

```json
{
    "inputs": [],
    "servers": {
        "echo_mcp": {
            "args": [],
            "env": {}
        }
    }
}
```

Or using the inspector: `npx @modelcontextprotocol/inspector node build/index.js`
