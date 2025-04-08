using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a PostgreSQL container.
/// </summary>
public class MCPInspectorResource
    : ContainerResource,
        IResourceWithEnvironment,
        IResourceWithServiceDiscovery
{
    internal const string PrimaryEndpointName = "http";

    /// <summary>
    /// Initializes a new instance of the <see cref="MCPInspectorResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public MCPInspectorResource(
        string name,
        string? tag = null,
        int? serverPort = 6277,
        int? clientPort = 6274
    )
        : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        ServerPort = serverPort;
        ClientPort = clientPort;
    }

    /// <summary>
    /// Gets the primary endpoint for the MCPInspector server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }
    public int? ServerPort { get; }
    public int? ClientPort { get; }
    public string? Tag { get; set; }
    public string BaseName { get; init; }
}
