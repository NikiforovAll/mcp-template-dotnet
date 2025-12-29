using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Mcp.Template.Tests;

/// <summary>
/// Checkpoint 4: Tests for authenticated MCP servers (MCPServerAuth)
/// Verifies server enforces authentication and returns 401 Unauthorized
/// </summary>
public class AuthServerTests : IAsyncLifetime
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
    public async Task MCPServerAuth_WithoutCredentials_Returns401()
    {
        // Arrange
        _port = GetAvailablePort();
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerAuth"
        );

        _serverProcess = StartServer(projectPath, _port);
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_port}") };

        await WaitForServerAsync(_httpClient, TimeSpan.FromSeconds(30));

        // MCP initialize request (JSON-RPC 2.0) without auth
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = content };
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
        );

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert - Should return 401 Unauthorized (no auth token provided)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MCPServerAuth_WithoutCredentials_ReturnsWwwAuthenticate()
    {
        // Arrange
        _port = GetAvailablePort();
        var projectPath = Path.Combine(
            SolutionRoot,
            "src",
            "templates",
            "content",
            "MCPServerAuth"
        );

        _serverProcess = StartServer(projectPath, _port);
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{_port}") };

        await WaitForServerAsync(_httpClient, TimeSpan.FromSeconds(30));

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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = content };
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
        );

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert - Should include WWW-Authenticate header with MCP auth scheme
        Assert.True(
            response.Headers.WwwAuthenticate.Count > 0,
            "Expected WWW-Authenticate header in 401 response"
        );
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

        // Set required Azure AD config via environment variables
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["McpServerUrl"] = $"http://localhost:{port}";
        psi.Environment["AzureAd__Instance"] = "https://login.microsoftonline.com/";
        psi.Environment["AzureAd__TenantId"] = "00000000-0000-0000-0000-000000000000";
        psi.Environment["AzureAd__ClientId"] = "00000000-0000-0000-0000-000000000001";
        psi.Environment["AzureAd__Audience"] = "api://00000000-0000-0000-0000-000000000001";

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
                using var socket = new System.Net.Sockets.TcpClient();
                await socket.ConnectAsync(client.BaseAddress!.Host, client.BaseAddress.Port);
                return;
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
