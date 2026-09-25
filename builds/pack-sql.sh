#!/usr/bin/env bash

# Packs every SQL (.sqlproj) package under libs/ in dependency order.
#
# Usage:   ./builds/pack-sql.sh <output-dir> [extra dotnet pack args...]
# Example: ./builds/pack-sql.sh ./artifacts/nuget --configuration Release /p:Version=10.0.65
#
# SQL packages reference each other at $(Version) (e.g. Notifications -> Security -> Users),
# so a package can only restore once everything it references is already packed into
# <output-dir>. The script packs in passes, each pass packing the projects whose same-repo
# dependencies are done, so any depth of dependency chain works. <output-dir> must be a
# restore source for this to resolve — a configured NuGet source (`dotnet nuget add source`)
# or -p:RestoreAdditionalProjectSources=<output-dir> in the extra args.
#
# A .sqlproj's package id is its file name (none of them set <PackageId>).
# Written for bash 3.2 (macOS /bin/bash): no associative arrays.

set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: ./builds/pack-sql.sh <output-dir> [extra dotnet pack args...]"
    exit 1
fi

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$1"
OUT="$(cd "$1" && pwd)"
shift

cd "$REPO_ROOT"
REMAINING=$(find libs -name '*.sqlproj' -not -path '*/obj/*' -not -path '*/bin/*' | sort)

# Space-delimited id sets, matched with case patterns.
ALL_IDS=" "
for proj in $REMAINING; do
    ALL_IDS="$ALL_IDS$(basename "$proj" .sqlproj) "
done
PACKED=" "

while [ -n "$REMAINING" ]; do
    NEXT=""
    for proj in $REMAINING; do
        ready=1
        for dep in $(grep -o 'PackageReference Include="Sencilla[^"]*"' "$proj" | cut -d'"' -f2); do
            # Only same-repo SQL packages gate the order; anything else restores from the feeds.
            case "$ALL_IDS" in *" $dep "*) ;; *) continue ;; esac
            case "$PACKED" in *" $dep "*) ;; *) ready=0 ;; esac
        done

        if [ "$ready" -eq 0 ]; then
            NEXT="$NEXT$proj"$'\n'
            continue
        fi

        echo "Packing $proj"
        dotnet pack "$proj" --output "$OUT" /p:GeneratePackageOnBuild=false "$@"
        PACKED="$PACKED$(basename "$proj" .sqlproj) "
    done

    NEXT="${NEXT%$'\n'}"
    if [ "$NEXT" = "$REMAINING" ]; then
        echo "Error: circular PackageReferences between SQL projects:"
        echo "$NEXT"
        exit 1
    fi
    REMAINING="$NEXT"
done
