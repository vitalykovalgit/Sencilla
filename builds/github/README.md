# GitHub Actions Pipelines

This folder mirrors the active GitHub Actions workflow files.
**The canonical location that GitHub reads is `.github/workflows/`** — always keep both in sync.

## Files

| File | Workflow | Trigger |
| ---- | -------- | ------- |
| `build.yml` | CI Build | Push to `master`/`main`/`develop`/`release/**`, pull requests |
| `release.yml` | Release NuGet Packages | Push of any `v*` tag |

---

## Workflow: CI Build (`build.yml`)

Runs on every push and pull request to main branches.

**Steps:**
1. Checkout code
2. Compute a CI build version (`<base>-ci.<run_number>`)
3. Setup .NET 10 SDK
4. `dotnet restore`
5. `dotnet build --configuration Release`
6. `dotnet test` — publishes results via `dorny/test-reporter`
7. `dotnet pack` — verifies packaging compiles (packages are **not** published)

---

## Workflow: Release (`release.yml`)

Triggered when you push a version tag. This is how you release.

**Steps:**
1. Checkout code
2. Extract version from tag (strips leading `v`): `v10.0.3` → `10.0.3`
3. Setup .NET 10 SDK
4. `dotnet restore`
5. `dotnet build --configuration Release /p:Version=<version>`
6. `dotnet test`
7. `dotnet pack` — produces `.nupkg` and `.snupkg` for ALL projects
8. Upload packages as GitHub artifact
9. `dotnet nuget push` → `https://api.nuget.org/v3/index.json`
10. Push symbol packages (`.snupkg`)
11. Create GitHub Release with auto-generated notes

---

## How to Make a Release

```bash
# 1. Commit and push all your changes
git add .
git commit -m "Prepare release 10.0.3"
git push

# 2. Create an annotated tag
git tag -a v10.0.3 -m "Release 10.0.3"

# 3. Push the tag — this triggers the release workflow
git push origin v10.0.3
```

All NuGet packages in the solution are packed with version `10.0.3` and pushed to NuGet.org.

---

## Pre-release Versions

Use SemVer pre-release suffixes in your tag:

| Tag | Package version | Use case |
| --- | --------------- | -------- |
| `v10.0.3` | `10.0.3` | Stable release |
| `v10.0.3-rc.1` | `10.0.3-rc.1` | Release candidate |
| `v10.0.3-beta.2` | `10.0.3-beta.2` | Beta |
| `v10.0.3-alpha.1` | `10.0.3-alpha.1` | Alpha |

NuGet.org marks anything with a pre-release suffix as a pre-release package automatically.

---

## Required GitHub Secret

Before the release workflow can push to NuGet.org, you must add your API key as a repository secret:

1. Go to **Settings → Secrets and variables → Actions** in your GitHub repository
2. Click **New repository secret**
3. Name: `NUGET_API_KEY`
4. Value: your NuGet.org API key (get it from https://www.nuget.org/account/apikeys)

---

## Adding New Projects

When you add a new library project to the solution, no workflow changes are needed. The `dotnet pack Sencilla.sln` command automatically picks up all packable projects.

To **exclude** a project from packaging, add this to its `.csproj`:

```xml
<PropertyGroup>
  <IsPackable>false</IsPackable>
</PropertyGroup>
```

Test projects and apps are automatically excluded because they lack `<IsPackable>true</IsPackable>`.

---

## Local Testing of Workflows

Install [act](https://github.com/nektos/act) to run workflows locally:

```bash
brew install act

# Simulate a push to master
act push --eventpath .github/events/push.json

# Simulate a tag push
act push --eventpath .github/events/tag-push.json
```

For local package development without pushing a tag, see [../local/README.md](../local/README.md).
