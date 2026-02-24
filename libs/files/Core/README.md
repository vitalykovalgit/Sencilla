# Sencilla.Component.Files

File storage abstraction for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla). Provides a provider-agnostic interface for file upload, download, and management.

## Available Providers

| Package | Storage |
|---------|---------|
| `Sencilla.Component.Files.AmazonS3` | Amazon S3 |
| `Sencilla.Component.Files.AzureStorage` | Azure Blob Storage |
| `Sencilla.Component.Files.GoogleStorage` | Google Cloud Storage |
| `Sencilla.Component.Files.LocalDrive` | Local file system |
| `Sencilla.Component.Files.Database` | Database (EF Core) |

## Installation

```bash
dotnet add package Sencilla.Component.Files
```

## Documentation

- [Files](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/files/README.md)

## License

MIT
