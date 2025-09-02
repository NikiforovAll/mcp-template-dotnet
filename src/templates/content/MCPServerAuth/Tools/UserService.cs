namespace MCPServerAuth.Tools;

public class UserService(IHttpContextAccessor httpContextAccessor)
{
    public string? UserName => httpContextAccessor.HttpContext?.User.Identity?.Name;
}
