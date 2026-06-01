#!/bin/bash

# Packs and publishes a single .sqlproj or .csproj package to NuGet.org.
#
# Usage:   ./builds/publish-package.sh <project-path> <version> [api-key]
# Example: ./builds/publish-package.sh libs/components/Users/Database/Sencilla.Component.Users.Mssql.sqlproj 10.0.18
#
# If api-key is omitted the script reads NUGET_API_KEY from the environment.

set -euo pipefail

PROJ="${1:-}"
VERSION="${2:-}"
API_KEY="${3:-${NUGET_API_KEY:-}}"

if [ -z "$PROJ" ] || [ -z "$VERSION" ]; then
    echo "Usage: ./builds/publish-package.sh <project-path> <version> [api-key]"
    echo "Example: ./builds/publish-package.sh libs/components/Users/Database/Sencilla.Component.Users.Mssql.sqlproj 10.0.18"
    exit 1
fi

if [ -z "$API_KEY" ]; then
    echo "Error: NuGet API key required. Pass it as the third argument or set NUGET_API_KEY."
    exit 1
fi

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$REPO_ROOT/artifacts/nuget-single"

echo "Packing $PROJ @ $VERSION ..."
dotnet pack "$REPO_ROOT/$PROJ" \
    --configuration Release \
    --output "$OUT" \
    /p:Version="$VERSION" \
    /p:GeneratePackageOnBuild=false

echo ""
echo "Produced packages:"
ls -lh "$OUT/"

echo ""
echo "Pushing to NuGet.org ..."
dotnet nuget push "$OUT/*.nupkg" \
    --api-key "$API_KEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate

echo ""
echo "Done."
