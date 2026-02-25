# Files

[Home](../../README.md) / [Docs](../index.md) / Files

**NuGet:** `Sencilla.Component.Files` (core) + provider package
**Source:** `libs/files/`

Sencilla Files provides a **provider-agnostic file storage abstraction** with resumable uploads (TUS protocol), multi-resolution image support, and unified file management across local, cloud, and database backends.

---

## Architecture

```text
Sencilla.Component.Files (Core)
├── Entity/          File, FileUpload, FileFilter, FileOrigin
├── Contract/        IFileStorage, IFileRequestHandler, IFilePathResolver
├── Impl/            CreateFileHandler, UploadFileHandler, HeadFileHandler, DeleteFileHandler
├── Web/             FileStreamController (download/stream endpoints)
├── Events/          FileCreatedEvent, FileUploadedEvent, FileDeletedEvent
├── Extension/       HttpContext helpers, metadata parsing, MIME type map
└── Bootstrap.cs     Service registration

Storage Providers (separate packages):
├── Sencilla.Component.Files.LocalDrive      File system
├── Sencilla.Component.Files.AzureStorage    Azure Blob Storage
├── Sencilla.Component.Files.AmazonS3        AWS S3
├── Sencilla.Component.Files.GoogleStorage   Google Cloud Storage
└── Sencilla.Component.Files.Database        EF Core (DB-stored files)
```

---

## Packages

| Package | Backend | When to use |
| ------- | ------- | ----------- |
| `Sencilla.Component.Files` | -- | Core interfaces (always required) |
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
// In Program.cs or Startup.cs
builder.Services.AddSencillaFiles(options =>
    options.UseLocalDrive(path: "/var/app/uploads"));

// Enable TUS resumable upload middleware
app.UseTusResumableUpload("/api/v1/files/upload");
```

### 2. Configuration-based setup

```csharp
builder.Services.AddSencillaFiles(options =>
    options.UseLocalDrive(builder.Configuration));
```

```json
{
  "SencillaFiles": {
    "LocalDrive": {
      "RootPath": "/var/app/uploads",
      "UseAsDefault": true
    },
    "Dirs": {
      "User": "user{0}"
    }
  }
}
```

### 3. Azure Blob Storage

```csharp
builder.Services.AddSencillaFiles(options =>
    options.UseAzureStorage(builder.Configuration));
```

---

## File Entity

The `File` entity represents a stored file with full metadata:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Id` | `Guid` | Unique file identifier |
| `ParentId` | `Guid?` | Reference to original file (for dimension variants) |
| `Name` | `string` | Original file name |
| `MimeType` | `string?` | MIME type (auto-detected from extension if not provided) |
| `Size` | `long` | File size in bytes |
| `Uploaded` | `long` | Bytes uploaded so far (for resumable uploads) |
| `UserId` | `int?` | Owner user ID |
| `Origin` | `FileOrigin` | `None`, `System`, or `User` |
| `Path` | `string?` | Storage path (auto-generated) |
| `Storage` | `byte` | Storage provider type identifier |
| `Dim` | `int?` | Image dimension (for resized variants) |
| `Width` | `int?` | Image width in pixels |
| `Height` | `int?` | Image height in pixels |
| `Resolutions` | `int[]?` | Available dimension variants (JSON array, set on parent file) |
| `Attrs` | `IDictionary<string,string>?` | Custom attributes (projectId, folder, etc.) |
| `CreatedDate` | `DateTime` | Creation timestamp |
| `UpdatedDate` | `DateTime` | Last update timestamp |
| `DeletedDate` | `DateTime?` | Soft-delete timestamp |

The entity implements `IEntityCreateable`, `IEntityUpdateable`, `IEntityRemoveable`, and `IEntityDeleteable`, providing full CRUD support via the auto-generated `[CrudApi("api/v1/files")]` endpoints.

---

## API Endpoints

### TUS Resumable Upload Protocol

The upload system implements the [TUS resumable upload protocol](https://tus.io/), enabling reliable file uploads with pause/resume support.

#### POST - Create file record

```http
POST /api/v1/files/upload
Headers:
  Upload-Length: <file-size-in-bytes>
  Upload-Defer-Length: 1
  Upload-Metadata: name <base64>, size <base64>, mimetype <base64>, ...

Response: 201 Created
  Location: /api/v1/files/upload/<file-id>
```

**Metadata keys** (base64-encoded values):

| Key | Required | Description |
| --- | -------- | ----------- |
| `name` | Yes | File name |
| `size` | No | File size (defaults to Upload-Length) |
| `mimetype` | No | MIME type (auto-detected from name) |
| `id` | No | Custom file ID (auto-generated if omitted) |
| `dim` | No | Image dimension for variants |
| `parentid` | No | Parent file ID (for multi-resolution) |
| `origin` | No | `User` (default) or `System` |
| `userid` | No | Owner user ID |
| `width` | No | Image width |
| `height` | No | Image height |
| `projectid` | No | Project ID (stored in Attrs, used in path) |
| `folder` | No | Subfolder name (stored in Attrs, used in path) |

Any additional keys are stored in the `Attrs` dictionary.

#### PATCH - Upload file chunk

```http
PATCH /api/v1/files/upload/<file-id>
Headers:
  Upload-Offset: <current-byte-offset>
Body: <binary-chunk>

Response: 204 No Content
  Upload-Offset: <new-byte-offset>
```

#### HEAD - Check upload status

```http
HEAD /api/v1/files/upload/<file-id>
Headers:
  Tus-Resumable: 1.0.0

Response: 200 OK
  Upload-Length: <total-size>
  Upload-Offset: <uploaded-bytes>
```

#### DELETE - Delete file and all variants

```http
DELETE /api/v1/files/upload/<file-id>

Response: 204 No Content
```

Deletes the file from both storage and database. If the file has dimension variants, all variants are deleted as well.

### File Stream Endpoints

#### GET - Download file by ID with optional dimension

```http
GET /api/v1/files/stream/<file-id>?dim=<dimension>
```

Returns the file stream with appropriate content type. If `dim` is specified, returns the dimension variant. Response is cached (48 hours).

#### GET - Download file by exact ID

```http
GET /api/v1/files/stream/<file-id>/stream
```

Returns the file stream for the exact file ID (no dimension lookup).

---

## Multi-Resolution File Upload

Upload the same image at different resolutions (e.g., thumbnails, previews). Each resolution is stored as a separate file linked to the original via `ParentId`.

### Workflow

1. **Upload original file** (standard upload):

   ```http
   POST /api/v1/files/upload
   Upload-Metadata: name <base64(photo.jpg)>, size <base64(5242880)>

   Response: 201 Created
   Location: /api/v1/files/upload/aaa-bbb-ccc
   ```

2. **Upload dimension variant** (same file, different resolution):

   ```http
   POST /api/v1/files/upload
   Upload-Metadata: name <base64(photo.jpg)>, size <base64(102400)>,
                     parentid <base64(aaa-bbb-ccc)>, dim <base64(200)>,
                     width <base64(200)>, height <base64(150)>

   Response: 201 Created
   Location: /api/v1/files/upload/ddd-eee-fff
   ```

3. **Upload another dimension**:

   ```http
   POST /api/v1/files/upload
   Upload-Metadata: name <base64(photo.jpg)>, size <base64(51200)>,
                     parentid <base64(aaa-bbb-ccc)>, dim <base64(100)>
   ```

### How it works

- When `parentid` and `dim` are both provided, the handler creates a **dimension variant**
- A new file record is created with its own `Id`, `ParentId` pointing to the original, and the specified `Dim`
- The original file's `Resolutions` array is updated to include the new dimension (e.g., `[200, 100]`)
- If a variant with the same dimension already exists, the existing variant is returned (idempotent)
- Storage path includes the dimension suffix: `{fileId}_{dim}px{extension}` (e.g., `abc123_200px.jpg`)

### Retrieve by dimension

```http
GET /api/v1/files/stream/aaa-bbb-ccc?dim=200
```

This queries files with `ParentId = aaa-bbb-ccc` and `Dim = 200`.

---

## File Deletion

Delete a file and all its dimension variants in a single request:

```http
DELETE /api/v1/files/upload/<file-id>
```

The delete handler:

1. Finds the file by ID
2. Queries all dimension variants (`ParentId = file-id`)
3. Deletes each variant from storage and database
4. Deletes the original file from storage and database
5. Publishes `FileDeletedEvent`
6. Returns `204 No Content`

---

## Storage Path Resolution

Files are stored with paths determined by their origin and attributes:

| Origin | Path Pattern |
| ------ | ------------ |
| `User` | `user{userId}/project{projectId}/{folder}/{fileId}_{dim}px{ext}` |
| `System` | `system/{fileId}_{dim}px{ext}` |
| `None` | `none/{fileId}_{dim}px{ext}` |

- `_{dim}px` suffix is only added when `Dim` is set
- `project{projectId}` segment is only added when `projectId` attribute exists
- `{folder}` segment is only added when `folder` attribute exists
- Special characters in folder names are stripped (only `a-z`, `A-Z`, `0-9`, `_`, `-` allowed)

---

## IFileStorage Interface

All storage providers implement this interface:

```csharp
public interface IFileStorage
{
    byte Type { get; }

    // Directory operations
    string GetDirectory(string type, params object[] @params);
    string GetRootDirectory();
    Task CreateDirectoryAsync(string path);
    Task DeleteDirectoryAsync(string path);
    Task<string> RenameDirectoryAsync(string sourceDir, string destDir, CancellationToken token = default);
    Task<File[]> GetDirectoryEntriesAsync(string folder, CancellationToken token = default);

    // File read/write
    Stream OpenFileStream(File file, long offset = 0, CancellationToken token = default);
    Stream OpenFileStream(string path, long offset = 0, CancellationToken token = default);
    Task<Stream?> ReadFileAsync(File file, CancellationToken token = default);
    Task<Stream?> ReadFileAsync(string file, CancellationToken token = default);
    Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken token = default);
    Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken token = default);

    // File management
    Task DeleteFileAsync(string file, CancellationToken token = default);
    Task<File?> DeleteFileAsync(File? file, CancellationToken token = default);
    Task<bool> SaveFile(string file, Stream stream);
    Task<bool> SaveFile(string file, byte[] data);
    Task<bool> CopyToFileAsync(File srcFile, string dstPath);
    Task<bool> CopyToFileAsync(string srcPath, string dstPath);

    // Compression
    Task ZipFolderAsync(string folderToArchive, string destinationFile, CancellationToken token = default);
    Task AddFilesToZipAsync(string zipFilePath, IEnumerable<string> filesToAdd, string prefixToStrip = "/published", CancellationToken token = default);
}
```

---

## Events

The file system publishes domain events that can be handled by other components:

| Event | Published when |
| ----- | -------------- |
| `FileCreatedEvent` | A new file record is created (POST handler) |
| `FileUploadedEvent` | File upload is complete (`Uploaded == Size`) |
| `FileDeletedEvent` | A file is deleted (DELETE handler) |

Each event contains the `File` entity.

```csharp
public class MyFileHandler : IEventHandler<FileUploadedEvent>
{
    public Task Handle(FileUploadedEvent @event, CancellationToken token)
    {
        var file = @event.File;
        // Process uploaded file (e.g., generate thumbnails, scan for viruses)
        return Task.CompletedTask;
    }
}
```

---

## Querying Files

Use `FileFilter` to query files:

```csharp
// Get all dimension variants of a file
var variants = await fileRepo.GetAll(
    new FileFilter().ByParentId(originalFileId));

// Get a specific dimension variant
var thumbnail = await fileRepo.FirstOrDefault(
    new FileFilter().ByParentId(originalFileId).ByDimmension(200));

// Get files by ID
var files = await fileRepo.GetAll(
    new FileFilter().ById(fileId1, fileId2));
```

---

## See Also

- [Components](README.md) -- other pre-built modules
- [Config Component](../components/README.md#config) -- runtime configuration

---

[Home](../../README.md) / [Docs](../index.md) / **Files**
