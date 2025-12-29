using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Mcp.Template.Tests;

/// <summary>
/// Checkpoint 3: Tests for HTTP-based MCP servers (MCPServerRemote)
/// Uses Streamable HTTP transport (2025-03-26 spec) instead of deprecated SSE
/// </summary>
public class HttpServerTests : IAsyncLifetime
{
    private static readonly string SolutionRoot = TestHelpers.GetSolutionRoot();
    private Process? _serverProcess;
    private HttpClient? _httpClient;
    private int _port;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();

        if (_serverProcess is { HasExited: false })
        {
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }

        _serverProcess?.Dispose();
    }

    [Fact]
    public async Task MCPServerRemote_StreamableHttp_AcceptsInitialize()
    {
        // Arrange
        _port = GetAvailablePort();
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerRemote"
        );

        _serverProcess = StartServer(projectPath, _port);
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_port}") };

        await WaitForServerAsync(_httpClient, TimeSpan.FromSeconds(30));

        // MCP initialize request (JSON-RPC 2.0)
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "TestClient", version = "1.0.0" },
            },
        };

        var content = new StringContent(
            JsonSerializer.Serialize(initRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act - POST to root for Streamable HTTP transport
        using var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = content };
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
        );

        var response = await _httpClient.SendAsync(request);

        // Assert - Server accepts and processes the request
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("protocolVersion", responseBody);
        Assert.Contains("serverInfo", responseBody);
    }

    [Fact]
    public async Task MCPServerRemote_StreamableHttp_ListsTools()
    {
        // Arrange
        _port = GetAvailablePort();
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerRemote"
        );

        _serverProcess = StartServer(projectPath, _port);
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_port}") };

        await WaitForServerAsync(_httpClient, TimeSpan.FromSeconds(30));

        // Initialize first
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "TestClient", version = "1.0.0" },
            },
        };

        var initContent = new StringContent(
            JsonSerializer.Serialize(initRequest),
            Encoding.UTF8,
            "application/json"
        );

        using var initReq = new HttpRequestMessage(HttpMethod.Post, "/") { Content = initContent };
        initReq.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );
        initReq.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
        );

        var initResponse = await _httpClient.SendAsync(initReq);
        initResponse.Headers.TryGetValues("mcp-session-id", out var sessionIds);
        var sessionId = sessionIds?.FirstOrDefault();

        // List tools request
        var listToolsRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
        };

        var listContent = new StringContent(
            JsonSerializer.Serialize(listToolsRequest),
            Encoding.UTF8,
            "application/json"
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = listContent };
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
        );
        if (sessionId is not null)
            request.Headers.Add("mcp-session-id", sessionId);

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("tools", responseBody);
        Assert.Contains("Echo", responseBody);
    }

    private static Process StartServer(string projectPath, int port)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -v q -- --urls http://localhost:{port}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        return Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start server process");
    }

    private static async Task WaitForServerAsync(HttpClient client, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                // Try to connect to TCP port to verify server is listening
                using var socket = new System.Net.Sockets.TcpClient();
                await socket.ConnectAsync(client.BaseAddress!.Host, client.BaseAddress.Port);
                return; // Server is listening
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Server not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Server did not start within {timeout}");
    }

    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
