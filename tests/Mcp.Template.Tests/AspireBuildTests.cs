using System.Diagnostics;

namespace Mcp.Template.Tests;

/// <summary>
/// Checkpoint 2: Verify Aspire AppHost projects build successfully
/// </summary>
public class AspireBuildTests
{
    private static readonly string SolutionRoot = TestHelpers.GetSolutionRoot();

    [Fact]
    public async Task AppHost_Builds_Successfully()
    {
        var projectPath = Path.Combine(SolutionRoot, "samples", "AppHost", "AppHost.csproj");
        var result = await BuildProjectAsync(projectPath);

        Assert.True(
            result.Success,
            $"Build failed with exit code {result.ExitCode}:\n{result.Output}"
        );
    }

    [Fact]
    public async Task AppHostHybrid_Builds_Successfully()
    {
        var projectPath = Path.Combine(
            SolutionRoot,
            "samples",
            "AppHostHybrid",
            "AppHostHybrid.csproj"
        );
        var result = await BuildProjectAsync(projectPath);

        Assert.True(
            result.Success,
            $"Build failed with exit code {result.ExitCode}:\n{result.Output}"
        );
    }

    [Fact]
    public async Task AppHostRemote_Builds_Successfully()
    {
        var projectPath = Path.Combine(
            SolutionRoot,
            "samples",
            "AppHostRemote",
            "AppHostRemote.csproj"
        );
        var result = await BuildProjectAsync(projectPath);

        Assert.True(
            result.Success,
            $"Build failed with exit code {result.ExitCode}:\n{result.Output}"
        );
    }

    private static async Task<BuildResult> BuildProjectAsync(string projectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" --no-restore -v q",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new BuildResult(
            process.ExitCode == 0,
            process.ExitCode,
            string.IsNullOrEmpty(error) ? output : $"{output}\n{error}"
        );
    }

    private record BuildResult(bool Success, int ExitCode, string Output);
}
