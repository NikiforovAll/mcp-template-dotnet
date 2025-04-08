# MCPServerHybrid

## Overview
This is a Model Context Protocol (MCP) server implementation built with .NET 9.0. The MCP server provides a communication protocol for facilitating interactions between various components in a model-driven system. This implementation demonstrates how to set up a basic MCP server with custom tools and services.

## Run Locally

### SSE Mode

```bash
npx @modelcontextprotocol/inspector
```

Open 'SSE' tab and configture '/sse` endpoint in the inspector.

### Stdio Mode

```bash
npx @modelcontextprotocol/inspector dotnet run -v q -- --stdio
```

## Distribute as .NET Tool

Pack from the project directory:

```bash
dotnet pack -o Artefacts -c Release
```

Install the tool globally:

```bash
dotnet tool install --global --add-source ./Artefacts MCPServerHybrid
```

Now, after you installed this tool globally, you can run it from anywhere on your system. The tool will be available as `MCPServerHybrid` in your terminal.

💡 You can also create local tool manifest and install MCPs as tools locally.

Run the inspector:

```bash
npx @modelcontextprotocol/inspector -e DOTNET_ENVIRONMENT=Production MCPServerHybrid
```