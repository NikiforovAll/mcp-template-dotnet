namespace Mcp.Template.Tests;

internal static class TestHelpers
{
    public static string GetSolutionRoot()
    {
        var assemblyDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(assemblyDir);

        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0 || current.GetFiles("*.slnx").Length > 0)
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not find solution root from {assemblyDir}");
    }
}
