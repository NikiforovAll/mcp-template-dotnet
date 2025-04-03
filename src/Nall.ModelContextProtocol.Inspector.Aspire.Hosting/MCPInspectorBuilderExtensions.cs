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
        string name = "MCPInspector",
        string? tag = null,
        int? port = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var server = new MCPInspectorResource(name);

        return builder
            .AddResource(server)
            .WithHttpEndpoint(
                port: port ?? DefaultContainerPort,
                targetPort: DefaultContainerPort,
                name: MCPInspectorResource.PrimaryEndpointName
            )
            .WithImage(
                MCPInspectorContainerImageTags.Image,
                tag ?? MCPInspectorContainerImageTags.Tag
            )
            .WithImageRegistry(MCPInspectorContainerImageTags.Registry);
    }

    public static IResourceBuilder<MCPInspectorResource> WithSSE(
        this IResourceBuilder<MCPInspectorResource> builder,
        IResourceBuilder<ProjectResource> project
    )
    {
        // TOOD:
        // builder.WithArgs();
        // project.WithExplicitStart();

        return builder;
    }
}
