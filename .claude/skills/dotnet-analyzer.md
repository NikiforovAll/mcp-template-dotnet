# .NET Analyzer — Find and Fix Diagnostics

Use this skill when the user asks to find, check, or fix .NET analyzer warnings/errors (CA, IDE, CS rules).

## Two Diagnostic Categories

.NET has **two separate diagnostic pipelines**. You MUST run both:

### 1. Build-time analyzers (CA/CS rules)

These run during `dotnet build`. To see them:

```bash
dotnet build 2>&1 | grep -E "warning|error"
```

Fix by: editing code, adding `<NoWarn>` in csproj, or adjusting `.editorconfig` severity.

### 2. IDE analyzers (IDE rules — IDE0065, IDE1006, IDE0011, etc.)

These do NOT run during `dotnet build` by default. Use `dotnet format`:

```bash
# Check all style violations
dotnet format style --severity warn --verify-no-changes -v diag 2>&1 | grep "IDE"

# Check specific diagnostic
dotnet format style --diagnostics IDE0065 --severity warn --verify-no-changes 2>&1

# Auto-fix (use with care)
dotnet format style --diagnostics IDE0065 --severity warn
```

For third-party analyzers:

```bash
dotnet format analyzers --severity warn --verify-no-changes -v diag 2>&1 | grep "warning\|error"
```

## Workflow

1. **Discover** — run both pipelines to get the full picture
2. **Categorize** — group by diagnostic ID
3. **Decide** — for each group:
   - **Fix the code** if it's a real issue (most cases)
   - **Adjust .editorconfig** if the rule doesn't match project style conventions
   - **Add NoWarn in csproj** only for false positives in specific projects (e.g., template content)
4. **Verify** — re-run both pipelines to confirm zero violations

## Quick Full Scan

```bash
# Build warnings (CA/CS)
dotnet build 2>&1 | grep -E "warning|error" | sort -u

# IDE style warnings
dotnet format style --severity warn --verify-no-changes -v diag 2>&1 | grep ": error\|: warning" | sort -u

# Third-party analyzer warnings
dotnet format analyzers --severity warn --verify-no-changes -v diag 2>&1 | grep ": error\|: warning" | sort -u
```

## Common Fixes

| Diagnostic | Issue | Typical Fix |
|---|---|---|
| IDE0065 | Using placement | Match `csharp_using_directive_placement` in .editorconfig |
| IDE1006 | Naming convention | Align naming styles in .editorconfig |
| IDE0011 | Missing braces | Add braces to if/else/for/while |
| CA1050 | Type not in namespace | Expected for top-level programs — suppress in csproj |
| CA1873 | Expensive logging arg | Use structured logging or suppress if false positive |

## Important Notes

- `dotnet format` severity flag uses `warn` not `warning`
- `--verify-no-changes` is dry-run mode (exits non-zero if changes needed)
- Always check `.editorconfig` rules before suppressing — the rule might just be misconfigured
- Template content projects (under `src/templates/content/`) may need different rules than main code
