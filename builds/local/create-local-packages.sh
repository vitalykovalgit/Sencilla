#!/usr/bin/env bash
# =============================================================================
# create-local-packages.sh
#
# Builds and publishes Sencilla NuGet packages to a local directory for
# development and testing. Use when you want to consume your changes
# from another project without pushing to NuGet.org.
#
# Usage:
#   ./create-local-packages.sh [OPTIONS]
#
# Options:
#   --version <ver>        Base version (default: read from Directory.Build.props)
#   --suffix <sfx>         Pre-release suffix (default: "local")
#   --local-dir <path>     Local NuGet feed directory (default: ~/local-nuget)
#   --configuration <cfg>  Build configuration (default: Release)
#   --exact-version        Use --version as-is, do not append suffix
#   --clean                Remove existing packages for this version before building
#   --help                 Show this help
#
# Examples:
#   # Defaults — auto-version, ~/local-nuget
#   ./create-local-packages.sh
#
#   # Specific version
#   ./create-local-packages.sh --version 10.0.3 --suffix dev
#
#   # Exact version
#   ./create-local-packages.sh --version 10.0.3-preview.1 --exact-version
#
#   # Custom directory + clean
#   ./create-local-packages.sh --local-dir /tmp/nuget-feed --clean
# =============================================================================

set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────
VERSION=""
SUFFIX="local"
LOCAL_NUGET_DIR="${HOME}/local-nuget"
CONFIGURATION="Release"
EXACT_VERSION=false
CLEAN=false

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)        VERSION="$2";            shift 2 ;;
        --suffix)         SUFFIX="$2";             shift 2 ;;
        --local-dir)      LOCAL_NUGET_DIR="$2";    shift 2 ;;
        --configuration)  CONFIGURATION="$2";      shift 2 ;;
        --exact-version)  EXACT_VERSION=true;      shift   ;;
        --clean)          CLEAN=true;              shift   ;;
        --help)
            sed -n '3,30p' "$0"   # print the header comment
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Run with --help for usage." >&2
            exit 1
            ;;
    esac
done

# ── Resolve repo root ─────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SOLUTION_FILE="$REPO_ROOT/Sencilla.sln"
ARTIFACTS_DIR="$REPO_ROOT/artifacts/nuget-local"

# ── Detect version from Directory.Build.props ─────────────────────────────────
if [[ -z "$VERSION" ]]; then
    BUILD_PROPS="$REPO_ROOT/Directory.Build.props"
    if [[ -f "$BUILD_PROPS" ]]; then
        VERSION=$(grep -oPm1 '(?<=<Version>)[^<]+' "$BUILD_PROPS" 2>/dev/null || echo "")
    fi
    VERSION="${VERSION:-10.0.0}"
fi

# ── Compute package version ───────────────────────────────────────────────────
if [[ "$EXACT_VERSION" == false ]]; then
    COUNTER=$(date +%Y%m%d%H%M)
    PACKAGE_VERSION="${VERSION}-${SUFFIX}.${COUNTER}"
else
    PACKAGE_VERSION="$VERSION"
fi

# ── Expand ~ in LOCAL_NUGET_DIR ───────────────────────────────────────────────
LOCAL_NUGET_DIR="${LOCAL_NUGET_DIR/#\~/$HOME}"

# ── Colors ────────────────────────────────────────────────────────────────────
CYAN='\033[0;36m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
RED='\033[0;31m'; GRAY='\033[0;37m'; NC='\033[0m'

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo -e "${CYAN}  Sencilla Local Package Builder${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo -e "  Package version : ${PACKAGE_VERSION}"
echo -e "  Configuration   : ${CONFIGURATION}"
echo -e "  Artifacts dir   : ${ARTIFACTS_DIR}"
echo -e "  Local feed dir  : ${LOCAL_NUGET_DIR}"
echo -e "  Solution        : ${SOLUTION_FILE}"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo ""

# ── Clean artifacts dir ───────────────────────────────────────────────────────
rm -rf "$ARTIFACTS_DIR"
mkdir -p "$ARTIFACTS_DIR"

# ── Create local feed dir ─────────────────────────────────────────────────────
if [[ ! -d "$LOCAL_NUGET_DIR" ]]; then
    echo -e "${YELLOW}Creating local NuGet feed directory: $LOCAL_NUGET_DIR${NC}"
    mkdir -p "$LOCAL_NUGET_DIR"
fi

# ── Clean old packages ────────────────────────────────────────────────────────
if [[ "$CLEAN" == true ]]; then
    echo -e "${YELLOW}Cleaning old packages from $LOCAL_NUGET_DIR ...${NC}"
    find "$LOCAL_NUGET_DIR" -name "Sencilla.*.nupkg" \
        -delete -print | sed 's/^/  Removed: /'
    echo ""
fi

# ── Restore ───────────────────────────────────────────────────────────────────
echo -e "${YELLOW}Restoring packages...${NC}"
dotnet restore "$SOLUTION_FILE"

# ── Build ─────────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}Building solution (${CONFIGURATION}, v${PACKAGE_VERSION})...${NC}"
dotnet build "$SOLUTION_FILE" \
    --configuration "$CONFIGURATION" \
    --no-restore \
    /p:Version="$PACKAGE_VERSION"

# ── Pack ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}Packing NuGet packages...${NC}"
dotnet pack "$SOLUTION_FILE" \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$ARTIFACTS_DIR" \
    /p:Version="$PACKAGE_VERSION"

# ── Copy to local feed ────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}Copying packages to local feed...${NC}"

PACKAGE_COUNT=0
while IFS= read -r -d '' pkg; do
    FILENAME=$(basename "$pkg")
    cp "$pkg" "$LOCAL_NUGET_DIR/$FILENAME"
    echo -e "  ${GREEN}✓ $FILENAME${NC}"
    PACKAGE_COUNT=$((PACKAGE_COUNT + 1))
done < <(find "$ARTIFACTS_DIR" -name "*.nupkg" -print0)

if [[ $PACKAGE_COUNT -eq 0 ]]; then
    echo -e "${RED}No packages found in $ARTIFACTS_DIR${NC}" >&2
    exit 1
fi

# ── Create nuget.local.config if it doesn't exist ────────────────────────────
NUGET_CONFIG="$REPO_ROOT/nuget.local.config"
if [[ ! -f "$NUGET_CONFIG" ]]; then
    echo ""
    echo -e "${YELLOW}Creating nuget.local.config...${NC}"
    cat > "$NUGET_CONFIG" << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Local Sencilla" value="${LOCAL_NUGET_DIR}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
    echo -e "  ${GREEN}Created: $NUGET_CONFIG${NC}"
    echo -e "  ${CYAN}Use with: dotnet restore --configfile nuget.local.config${NC}"
fi

# ── Done ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Done! ${PACKAGE_COUNT} package(s) published${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════${NC}"
echo ""
echo -e "To use in your project, add the local feed to your nuget.config:"
echo ""
echo -e "${GRAY}  <packageSources>${NC}"
echo -e "${GRAY}    <add key=\"Local Sencilla\" value=\"${LOCAL_NUGET_DIR}\" />${NC}"
echo -e "${GRAY}  </packageSources>${NC}"
echo ""
echo -e "Then reference the package:"
echo ""
echo -e "${GRAY}  dotnet add package Sencilla.Core --version ${PACKAGE_VERSION}${NC}"
echo ""
