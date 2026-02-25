#!/bin/bash

# Usage: ./builds/release.sh 10.0.8

if [ -z "$1" ]; then
    echo "Usage: ./builds/release.sh <version>"
    echo "Example: ./builds/release.sh 10.0.8"
    exit 1
fi

version="$1"
tag="v$version"

echo "Creating release $tag..."

git tag -a "$tag" -m "Release $version"
git push origin "$tag"

echo "Release $tag pushed to origin."
