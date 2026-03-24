using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace Mcp.Template.Tests;

public class TemplateInstantiationTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Theory]
    [InlineData("mcp-server", "MCPServer")]
    [InlineData("mcp-server-http", "MCPServerRemote")]
    [InlineData("mcp-server-hybrid", "MCPServerHybrid")]
    [InlineData("mcp-server-http-auth", "MCPServerAuth")]
    public async Task Template_Instantiates_And_Builds(string shortName, string templateFolder)
    {
        var templatePath = Path.GetFullPath(
            Path.Combine(
                TestHelpers.GetSolutionRoot(),
                "src",
                "templates",
                "content",
                templateFolder
            )
        );
        var outputDir = Path.Combine(
            Path.GetTempPath(),
            $"mcp-template-test-{shortName}-{Path.GetRandomFileName()}"
        );

        try
        {
            var options = new TemplateVerifierOptions(templateName: shortName)
            {
                TemplatePath = templatePath,
                TemplateSpecificArgs = [],
                OutputDirectory = outputDir,
                DisableDiffTool = true,
            }.WithCustomDirectoryVerifier(
                async (contentDirectory, contentFetcher) =>
                {
                    var csprojFiles = Directory.GetFiles(
                        contentDirectory,
                        "*.csproj",
                        SearchOption.AllDirectories
                    );
                    Assert.True(
                        csprojFiles.Length > 0,
                        $"Generated project should contain a .csproj file. Content dir: {contentDirectory}, files: {string.Join(", ", Directory.GetFiles(contentDirectory, "*", SearchOption.AllDirectories))}"
                    );

                    var projectDir = Path.GetDirectoryName(csprojFiles[0])!;
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "build -p:WarningLevel=0 /clp:ErrorsOnly",
                        WorkingDirectory = projectDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    var process = Process.Start(psi)!;
                    var stderr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    Assert.True(
                        process.ExitCode == 0,
                        $"dotnet build failed (exit code {process.ExitCode}):\n{stderr}"
                    );
                }
            );

            var engine = new VerificationEngine(this._logger);
            await engine.Execute(options);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Theory]
    [InlineData("mcp-server", "MCPServer", "TestProject")]
    [InlineData("mcp-server-http", "MCPServerRemote", "TestProject")]
    [InlineData("mcp-server-hybrid", "MCPServerHybrid", "TestProject")]
    [InlineData("mcp-server-http-auth", "MCPServerAuth", "TestProject")]
    public async Task Template_Name_Parameter_Renames_Project(
        string shortName,
        string templateFolder,
        string projectName
    )
    {
        var templatePath = Path.GetFullPath(
            Path.Combine(
                TestHelpers.GetSolutionRoot(),
                "src",
                "templates",
                "content",
                templateFolder
            )
        );
        var outputDir = Path.Combine(
            Path.GetTempPath(),
            $"mcp-template-test-rename-{shortName}-{Path.GetRandomFileName()}"
        );

        try
        {
            var options = new TemplateVerifierOptions(templateName: shortName)
            {
                TemplatePath = templatePath,
                TemplateSpecificArgs = ["-n", projectName],
                OutputDirectory = outputDir,
                DisableDiffTool = true,
            }.WithCustomDirectoryVerifier(
                async (contentDirectory, contentFetcher) =>
                {
                    var csprojFiles = Directory.GetFiles(
                        contentDirectory,
                        "*.csproj",
                        SearchOption.AllDirectories
                    );

                    Assert.True(csprojFiles.Length > 0, "Should contain a .csproj file");
                    Assert.Contains(
                        csprojFiles,
                        f => Path.GetFileName(f) == $"{projectName}.csproj"
                    );

                    await Task.CompletedTask;
                }
            );

            var engine = new VerificationEngine(this._logger);
            await engine.Execute(options);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }
}
