namespace MCPServerAuth.Tools;

using System.ComponentModel;
using ModelContextProtocol.Server;

/// <summary>
/// This is a simple tool that echoes the message back to the client.
/// </summary>
[McpServerToolType]
public class EchoTool(UserService userService)
{
    [McpServerTool(
        Name = "Echo",
        Title = "Echoes the message back to the client.",
        UseStructuredContent = true
    )]
    [Description("This tool echoes the message back to the client.")]
    public EchoResponse Echo(string message) =>
        new($"hello {message} from {userService.UserName}", userService.UserName!);
}

public record EchoResponse(string Message, string UserName);
