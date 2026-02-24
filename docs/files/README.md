# Files

[Home](../../README.md) / [Docs](../index.md) / Files

**NuGet:** `Sencilla.Component.Files` (core) + provider package
**Source:** `libs/files/`

> **Status:** Documentation in progress.

Sencilla Files provides a **provider-agnostic file storage abstraction**. Upload, download, and manage files with the same code regardless of whether you store them locally, in Azure Blob, S3, or Google Cloud.

---

## Packages

| Package | Backend | When to use |
| ------- | ------- | ----------- |
| `Sencilla.Component.Files` | — | Core interfaces (always required) |
| `Sencilla.Component.Files.LocalDrive` | File system | Development, on-premise |
| `Sencilla.Component.Files.AzureStorage` | Azure Blob Storage | Azure-hosted apps |
| `Sencilla.Component.Files.AmazonS3` | AWS S3 | AWS-hosted apps |
| `Sencilla.Component.Files.GoogleStorage` | Google Cloud Storage | GCP-hosted apps |
| `Sencilla.Component.Files.Database` | EF Core | Store files in DB (small files, audit trails) |

---

## Installation

```bash
dotnet add package Sencilla.Component.Files

# Choose one provider:
dotnet add package Sencilla.Component.Files.LocalDrive        # local dev
dotnet add package Sencilla.Component.Files.AzureStorage      # Azure
dotnet add package Sencilla.Component.Files.AmazonS3          # AWS
dotnet add package Sencilla.Component.Files.GoogleStorage      # GCP
dotnet add package Sencilla.Component.Files.Database          # database
```

---

## Quick Start

### 1. Register the provider

```csharp
// Local file system (development)
builder.Services.AddSencillaFiles(options =>
    options.UseLocalDrive(path: "/var/app/uploads"));

// Azure Blob Storage
builder.Services.AddSencillaFiles(options =>
    options.UseAzureStorage(
        connectionString: builder.Configuration["Azure:Storage:ConnectionString"],
        containerName: "uploads"));

// AWS S3
builder.Services.AddSencillaFiles(options =>
    options.UseAmazonS3(
        bucketName: "my-app-uploads",
        region: "us-east-1"));
```

### 2. Upload a file

```csharp
public class UploadService(IFileStorage storage)
{
    public async Task<string> UploadAsync(IFormFile file, CancellationToken token)
    {
        var key = $"products/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        await using var stream = file.OpenReadStream();
        await storage.UploadAsync(key, stream, file.ContentType, token);
        return key;
    }
}
```

### 3. Download a file

```csharp
public async Task<IActionResult> Download(string key)
{
    var stream = await storage.DownloadAsync(key);
    return File(stream, "application/octet-stream");
}
```

### 4. Delete a file

```csharp
await storage.DeleteAsync(key);
```

---

## `IFileStorage` Interface

```csharp
public interface IFileStorage
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken token = default);
    Task<Stream> DownloadAsync(string key, CancellationToken token = default);
    Task DeleteAsync(string key, CancellationToken token = default);
    Task<bool> ExistsAsync(string key, CancellationToken token = default);
    Task<string> GetUrlAsync(string key, CancellationToken token = default);
    Task<IEnumerable<string>> ListAsync(string prefix = "", CancellationToken token = default);
}
```

---

## Configuration Integration

`Sencilla.Component.Files` integrates with `Sencilla.Component.Config` to support runtime-configurable storage paths and bucket names:

```json
{
  "Files": {
    "Provider": "AzureStorage",
    "AzureStorage": {
      "ConnectionString": "...",
      "ContainerName": "uploads"
    }
  }
}
```

---

## Coming Soon

- Pre-signed URL generation (S3, Azure SAS tokens)
- File metadata storage
- Image resizing and thumbnail generation
- Virus scanning integration
- Multi-bucket routing (different buckets per entity type)

---

## See Also

- [Components](README.md) — other pre-built modules
- [Config Component](../components/README.md#config) — runtime configuration

---

[Home](../../README.md) / [Docs](../index.md) / **Files**
