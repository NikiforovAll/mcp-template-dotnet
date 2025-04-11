using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MCPInspector resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MCPInspectorBuilderExtensions
{
    private const int DefaultContainerPort = 6274;

    /// <summary>
    /// Adds a MCPInspector resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name"></param>
    /// <param name="tag"></param>
    /// <param name="port">The host port used when launching the container.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MCPInspectorResource> AddMCPInspector(
        this IDistributedApplicationBuilder builder,
        string name = "mcp-inspector",
        string? tag = null,
        int? serverPort = 6277,
        int? clientPort = 6274
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var server = new MCPInspectorResource(name + "-resource", tag, serverPort, clientPort)
        {
            BaseName = name,
        };

        return builder.AddResource(server);
    }

    public static IResourceBuilder<MCPInspectorResource> WithSSE(
        this IResourceBuilder<MCPInspectorResource> builder,
        IResourceBuilder<ProjectResource> project
    )
    {
        var outputDirectory = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );

        var nodeBuild = TryBuildInspector(builder, outputDirectory);

        var node = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.BaseName,
                Path.Combine(outputDirectory!, "inspector"),
                "start",
                []
            )
            .WithHttpEndpoint(
                isProxied: false,
                port: builder.Resource.ServerPort,
                env: "SERVER_PORT",
                name: "server-proxy"
            )
            .WithHttpEndpoint(
                isProxied: false,
                port: builder.Resource.ClientPort,
                env: "CLIENT_PORT",
                name: "client"
            )
            .WithUrlForEndpoint("client", u => u.DisplayText = "MCP Inspector")
            .WithUrlForEndpoint("server-proxy", u => u.DisplayText = "Server Proxy")
            .WaitForCompletion(nodeBuild)
            .ExcludeFromManifest();

        node.WithParentRelationship(builder.Resource);

        builder.WithInitialState(
            new CustomResourceSnapshot
            {
                ResourceType = "MCP Inspector",
                // State = KnownResourceStates.Hidden,
                State = KnownResourceStates.Hidden,
                Properties = [],
            }
        );

        builder.WithRelationship(project.Resource, "proxies");

        return builder;
    }

    public static IResourceBuilder<MCPInspectorResource> WithStdio<TProject>(
        this IResourceBuilder<MCPInspectorResource> builder
    )
        where TProject : IProjectMetadata, new()
    {
        var outputDirectory = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );
        IResourceBuilder<NodeAppResource>? nodeBuild = TryBuildInspector(builder, outputDirectory);

        var node = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.BaseName,
                Path.Combine(outputDirectory!, "inspector"),
                "start",
                [
                    "dotnet",
                    "run",
                    "--project",
                    $"'{new TProject().ProjectPath}'",
                    "--verbosity",
                    "quiet",
                    "--",
                    "--stdio",
                ]
            )
            .WithHttpEndpoint(
                isProxied: false,
                port: builder.Resource.ServerPort,
                env: "SERVER_PORT",
                name: "server-proxy"
            )
            .WithHttpEndpoint(
                isProxied: false,
                port: builder.Resource.ClientPort,
                env: "CLIENT_PORT",
                name: "client"
            )
            .WithUrlForEndpoint("client", u => u.DisplayText = "MCP Inspector")
            .WithUrlForEndpoint("server-proxy", u => u.DisplayText = "Server Proxy")
            .WaitForCompletion(nodeBuild)
            .ExcludeFromManifest();

        node.WithParentRelationship(builder.Resource);

        builder.WithInitialState(
            new CustomResourceSnapshot
            {
                ResourceType = "MCP Inspector",
                // State = KnownResourceStates.Hidden,
                State = KnownResourceStates.Hidden,
                Properties = [],
            }
        );

        return builder;
    }

    private static IResourceBuilder<NodeAppResource> TryBuildInspector(
        IResourceBuilder<MCPInspectorResource> builder,
        string? outputDirectory
    )
    {
        IResourceBuilder<NodeAppResource>? nodeBuild = null;
        if (
            builder
                .ApplicationBuilder.Resources.OfType<NodeAppResource>()
                .SingleOrDefault(x => x.Name == "inpector-node-build") is
            { } existingNodeBuild
        )
        {
            nodeBuild = builder.ApplicationBuilder.CreateResourceBuilder(existingNodeBuild);
        }
        else
        {
            nodeBuild = builder.ApplicationBuilder.AddNpmApp(
                "inpector-node-build",
                Path.Combine(outputDirectory!, "inspector"),
                "build"
            );
            // .WithInitialState(
            //     new CustomResourceSnapshot
            //     {
            //         ResourceType = "MCP Inspector",
            //         State = KnownResourceStates.Hidden,
            //         Properties = [],
            //     }
            // );

            builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
                nodeBuild.Resource,
                async (@event, cancellationToken) =>
                {
                    await NpmInstallAsync(outputDirectory!, @event.Services, nodeBuild);
                }
            );
        }

        return nodeBuild;
    }

    private static async Task NpmInstallAsync(
        string dir,
        IServiceProvider serviceProvider,
        IResourceBuilder<NodeAppResource> nodeBuild
    )
    {
        var logger = serviceProvider
            .GetRequiredService<ResourceLoggerService>()
            .GetLogger(nodeBuild.Resource);

        var npmPath = Path.Combine(dir, "inspector", "node_modules");

        if (!Directory.Exists(npmPath))
        {
            logger.LogInformation("Installing NPM modules...");
            var packageLockPath = Path.Combine(dir, "inspector", "package-lock.json");
            if (File.Exists(packageLockPath))
            {
                File.Delete(packageLockPath);
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? @"C:\Program Files\nodejs\npm.cmd"
                        : "/usr/local/bin/npm"
                )
                {
                    Arguments = "install",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(dir, "inspector"),
                },
            };

            // Redirect the output and error streams to the logger
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogError(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
        }
        else
        {
            logger.LogInformation(
                "📦 NPM modules are already installed. If something is wrong consider AppHost cleanup"
            );
        }
    }
}
