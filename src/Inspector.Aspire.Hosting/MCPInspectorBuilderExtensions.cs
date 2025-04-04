using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MCPInspector resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MCPInspectorBuilderExtensions
{
    private const int DefaultContainerPort = 5173; // 6274;

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
        int? port = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var server = new MCPInspectorResource(name + "-resource", tag, port) { BaseName = name };

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

        var nodeBuild = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.Name + "-node-build",
                Path.Combine(outputDirectory!, "inspector"),
                "build"
            )
            .WithInitialState(
                new CustomResourceSnapshot
                {
                    ResourceType = "MCP Inspector",
                    State = KnownResourceStates.Hidden,
                    Properties = [],
                }
            );

        var node = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.BaseName,
                Path.Combine(outputDirectory!, "inspector"),
                "start",
                []
            )
            .WithHttpEndpoint(
                port: builder.Resource.Port ?? DefaultContainerPort,
                name: MCPInspectorResource.PrimaryEndpointName
            )
            .WaitForCompletion(nodeBuild)
            .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            nodeBuild.Resource,
            async (@event, cancellationToken) =>
            {
                await NpmInstallAsync(outputDirectory!);
            }
        );

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

        // var primaryEndpoint = project.GetEndpoint("http");
        // builder
        //     .WithHttpEndpoint(
        //         port: builder.Resource.Port ?? DefaultContainerPort,
        //         targetPort: DefaultContainerPort,
        //         name: MCPInspectorResource.PrimaryEndpointName
        //     )
        //     .WithImage(
        //         MCPInspectorContainerImageTags.Image,
        //         builder.Resource.Tag ?? MCPInspectorContainerImageTags.Tag
        //     )
        //     .WithImageRegistry(MCPInspectorContainerImageTags.Registry)
        //     .WithEnvironment(
        //         "SSE_URL",
        //         ReferenceExpression.Create(
        //             $"{primaryEndpoint.Scheme}://{primaryEndpoint.Property(EndpointProperty.Host)}:{primaryEndpoint.Property(EndpointProperty.Port)}/sse"
        //         )
        //     )
        //     .WithReference(project);

        // return builder;
    }

    public static IResourceBuilder<MCPInspectorResource> WithStdio<TProject>(
        this IResourceBuilder<MCPInspectorResource> builder
    )
        where TProject : IProjectMetadata, new()
    {
        var outputDirectory = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );

        var nodeBuild = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.Name + "-node-build",
                Path.Combine(outputDirectory!, "inspector"),
                "build"
            )
            .WithInitialState(
                new CustomResourceSnapshot
                {
                    ResourceType = "MCP Inspector",
                    State = KnownResourceStates.Hidden,
                    Properties = [],
                }
            );

        var node = builder
            .ApplicationBuilder.AddNpmApp(
                builder.Resource.BaseName,
                Path.Combine(outputDirectory!, "inspector"),
                "start",
                [
                    "-e",
                    "DOTNET_Logging__LogLevel__Default=None",
                    "-e",
                    "DOTNET_Logging__Console__LogToStandardErrorThreshold=Trace",
                    "dotnet",
                    "run",
                    "--project",
                    $"'{new TProject().ProjectPath}'",
                    "--no-launch-profile",
                    "--verbosity",
                    "quiet",
                ]
            )
            .WaitForCompletion(nodeBuild)
            .ExcludeFromManifest();

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            nodeBuild.Resource,
            async (@event, cancellationToken) =>
            {
                await NpmInstallAsync(outputDirectory);
            }
        );

        node.WithParentRelationship(builder.Resource);

        builder
            .WithInitialState(
                new CustomResourceSnapshot
                {
                    ResourceType = "MCP Inspector",
                    // State = KnownResourceStates.Hidden,
                    State = KnownResourceStates.Hidden,
                    Properties = [],
                }
            )
            .WithHttpEndpoint(name: MCPInspectorResource.PrimaryEndpointName);

        return builder;
    }

    private static async Task NpmInstallAsync(string dir)
    {
        var npmPath = Path.Combine(dir, "inspector", "node_modules");

        if (!Directory.Exists(npmPath))
        {
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

            process.Start();

            await process.WaitForExitAsync();
        }
    }
}
