using MCPServerAuth.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserService>();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    })
    .AddMcp(options =>
    {
        var identityOptions = builder
            .Configuration.GetSection("AzureAd")
            .Get<MicrosoftIdentityOptions>()!;

        options.ResourceMetadata = new ProtectedResourceMetadata
        {
            Resource = GetMcpServerUrl(),
            AuthorizationServers = [GetAuthorizationServerUrl(identityOptions)],
            ScopesSupported = [$"api://{identityOptions.ClientId}/Mcp.User"],
        };
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddMcpServer().WithToolsFromAssembly().WithHttpTransport();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp().RequireAuthorization();

// Run the web server
app.Run();

// Helper method to get authorization server URL
static Uri GetAuthorizationServerUrl(MicrosoftIdentityOptions identityOptions) =>
    new($"{identityOptions.Instance?.TrimEnd('/')}/{identityOptions.TenantId}/v2.0");

Uri GetMcpServerUrl() => builder.Configuration.GetValue<Uri>("McpServerUrl") ?? throw new InvalidOperationException("McpServerUrl is not configured.");
