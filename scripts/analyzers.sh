#!/bin/bash
set -euo pipefail

ERRORS=""

# 1. Build-time analyzers (CA/CS rules)
BUILD_OUTPUT=$(dotnet build -p:WarningLevel=5 2>&1 | grep -E "warning|error" | grep -v "^Build" | sort -u || true)
if [ -n "$BUILD_OUTPUT" ]; then
    ERRORS+="Build warnings:\n$BUILD_OUTPUT\n\n"
fi

# 2. IDE style analyzers (IDE rules)
STYLE_OUTPUT=$(dotnet format style --severity warn --verify-no-changes -v diag 2>&1 | grep -E ": error |: warning " | sort -u || true)
if [ -n "$STYLE_OUTPUT" ]; then
    ERRORS+="IDE style violations:\n$STYLE_OUTPUT\n\n"
fi

if [ -n "$ERRORS" ]; then
    echo "{\"decision\": \"block\", \"reason\": \"Analyzer issues found. Fix them before finishing.\n\n$(echo -e "$ERRORS" | sed 's/"/\\"/g' | sed ':a;N;$!ba;s/\n/\\n/g')\"}"
    exit 0
fi

exit 0
