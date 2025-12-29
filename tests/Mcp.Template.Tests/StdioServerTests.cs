using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Mcp.Template.Tests;

/// <summary>
/// Checkpoint 1: Tests for stdio-based MCP servers (MCPServer, MCPServerHybrid --stdio)
/// </summary>
public class StdioServerTests : IAsyncLifetime
{
    private static readonly string SolutionRoot = GetSolutionRoot();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MCPServer_ListTools_ReturnsEchoTool()
    {
        // Arrange
        var projectPath = Path.Combine(SolutionRoot, "src", "templates", "content", "MCPServer");

        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = "MCPServer",
                    Command = "dotnet",
                    Arguments = ["run", "--project", projectPath, "-v", "q"],
                }
            )
        );

        // Act
        var tools = await client.ListToolsAsync();

        // Assert
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "echo_hello");
    }

    [Fact]
    public async Task MCPServer_CallEchoTool_ReturnsExpectedResponse()
    {
        // Arrange
        var projectPath = Path.Combine(SolutionRoot, "src", "templates", "content", "MCPServer");

        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = "MCPServer",
                    Command = "dotnet",
                    Arguments = ["run", "--project", projectPath, "-v", "q"],
                }
            )
        );

        // Act
        var result = await client.CallToolAsync(
            "echo_hello",
            new Dictionary<string, object?> { ["message"] = "world" }
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textContent);
        Assert.Contains("hello world", textContent.Text);
    }

    [Fact]
    public async Task MCPServerHybrid_Stdio_ListTools_ReturnsEchoTool()
    {
        // Arrange
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerHybrid"
        );

        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = "MCPServerHybrid",
                    Command = "dotnet",
                    Arguments = ["run", "--project", projectPath, "-v", "q", "--", "--stdio"],
                }
            )
        );

        // Act
        var tools = await client.ListToolsAsync();

        // Assert
        Assert.NotEmpty(tools);
        // MCPServerHybrid uses "echo" as tool name (method name without explicit Name property)
        Assert.Contains(tools, t => t.Name == "echo");
    }

    [Fact]
    public async Task MCPServerHybrid_Stdio_CallEchoTool_ReturnsExpectedResponse()
    {
        // Arrange
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerHybrid"
        );

        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = "MCPServerHybrid",
                    Command = "dotnet",
                    Arguments = ["run", "--project", projectPath, "-v", "q", "--", "--stdio"],
                }
            )
        );

        // Act - MCPServerHybrid uses "echo" as tool name
        var result = await client.CallToolAsync(
            "echo",
            new Dictionary<string, object?> { ["message"] = "test" }
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textContent);
        Assert.Contains("hello test", textContent.Text);
    }

    private static string GetSolutionRoot()
    {
        // Path from bin/Debug/net10.0 -> solution root (5 levels up)
        // tests/Mcp.Template.Tests/bin/Debug/net10.0 -> solution
        var assemblyDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(assemblyDir);

        // Walk up looking for .sln or .slnx file (new XML solution format)
        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0 || current.GetFiles("*.slnx").Length > 0)
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not find solution root from {assemblyDir}");
    }
}
