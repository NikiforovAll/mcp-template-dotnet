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
    public MCPInspectorResource(string name)
        : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
    }

    /// <summary>
    /// Gets the primary endpoint for the MCPInspector server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }
}
