# Local Development — NuGet Packages

Use these scripts to build Sencilla packages and publish them to a local NuGet directory. This lets you test changes in another project without pushing to NuGet.org.

---

## Quick Start

### macOS / Linux

```bash
# Defaults: auto-version, ~/local-nuget directory
./builds/local/create-local-packages.sh

# With specific version and suffix
./builds/local/create-local-packages.sh --version 10.0.3 --suffix dev

# Clean old versions before building
./builds/local/create-local-packages.sh --clean
```

### Windows (PowerShell)

```powershell
# Defaults: auto-version, ~/local-nuget directory
.\builds\local\create-local-packages.ps1

# With specific version and suffix
.\builds\local\create-local-packages.ps1 -Version "10.0.3" -Suffix "dev"

# Clean old versions before building
.\builds\local\create-local-packages.ps1 -Clean
```

---

## Parameters

| Parameter | Shell | PowerShell | Default | Description |
| --------- | ----- | ---------- | ------- | ----------- |
| Base version | `--version` | `-Version` | Read from `Directory.Build.props` | SemVer base (e.g. `10.0.3`) |
| Suffix | `--suffix` | `-Suffix` | `local` | Pre-release suffix |
| Local dir | `--local-dir` | `-LocalNugetDir` | `~/local-nuget` | Local NuGet feed path |
| Configuration | `--configuration` | `-Configuration` | `Release` | `Debug` or `Release` |
| Exact version | `--exact-version` | `-ExactVersion` | false | Use version as-is, no suffix |
| Clean | `--clean` | `-Clean` | false | Remove old packages first |

### Version Examples

| Command | Package version |
| ------- | --------------- |
| `--version 10.0.3` | `10.0.3-local.202502241130` |
| `--version 10.0.3 --suffix dev` | `10.0.3-dev.202502241130` |
| `--version 10.0.3-preview.1 --exact-version` | `10.0.3-preview.1` |
| (no args) | `10.0.2-local.202502241130` |

---

## Setting Up the Consumer Project

After running the script, configure your consumer project to use the local feed.

### Option A — `nuget.config` in the consumer project

```xml
<!-- nuget.config  (place next to your .csproj or .sln) -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Local Sencilla" value="~/local-nuget" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

### Option B — `nuget.local.config` (auto-generated)

The script creates `nuget.local.config` at the repo root on first run. Use it with:

```bash
dotnet restore --configfile nuget.local.config
```

### Option C — Global NuGet config

Add the local feed to your global NuGet config (applies to all projects):

```bash
dotnet nuget add source ~/local-nuget --name "Local Sencilla"
```

Remove when no longer needed:

```bash
dotnet nuget remove source "Local Sencilla"
```

---

## Consuming the Package

```bash
# Add with a specific preview version
dotnet add package Sencilla.Core --version "10.0.2-local.202502241130"

# Or let NuGet pick the latest (including pre-release)
dotnet add package Sencilla.Core --prerelease
```

```xml
<!-- Or directly in .csproj -->
<PackageReference Include="Sencilla.Core" Version="10.0.2-local.*" />
```

---

## Workflow: Developing a Change

1. **Make your changes** to a Sencilla library
2. **Run the build script** — produces versioned `.nupkg` files in `~/local-nuget`
3. **In your consumer project** — run `dotnet restore` (with your nuget.config pointing to the local feed)
4. **Test the change** in the consumer project
5. **Iterate** — re-run the script; the timestamp in the suffix ensures a new version is produced each time
6. When ready — create a tag to trigger the official release pipeline

---

## Troubleshooting

### NuGet still uses the old cached version

```bash
# Clear the global NuGet cache for Sencilla packages
dotnet nuget locals all --clear

# Or clear just the packages cache
rm -rf ~/.nuget/packages/sencilla.*
rm -rf ~/.nuget/packages/microsoft.entityframeworkcore.extension
```

The PowerShell script does this automatically with the `-Clean` flag.

### Package not found after building

Make sure your `nuget.config` points to the exact path used when building. Run:

```bash
dotnet nuget list source
```

and verify `Local Sencilla` is listed and the path is correct.

### Build fails with version format error

The auto-generated timestamp suffix (`-local.202502241130`) is always valid SemVer 2.0. If you supply a custom suffix, avoid special characters — only alphanumerics and dots are allowed.

---

## Directory Layout

```text
builds/
├── github/                    # GitHub Actions workflows (mirrored from .github/workflows/)
│   ├── build.yml
│   ├── release.yml
│   └── README.md
└── local/                     # This directory — local dev tooling
    ├── create-local-packages.ps1   # PowerShell
    ├── create-local-packages.sh    # Bash (macOS/Linux)
    └── README.md               # This file
```
