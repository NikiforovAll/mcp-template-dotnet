using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Mcp.Template.Tests;

/// <summary>
/// Checkpoint 1: Tests for stdio-based MCP servers (MCPServer, MCPServerHybrid --stdio)
/// </summary>
public class StdioServerTests : IAsyncLifetime
{
    private static readonly string SolutionRoot = TestHelpers.GetSolutionRoot();

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
}
