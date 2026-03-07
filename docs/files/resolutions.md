# File Resolutions

[Home](../../README.md) / [Docs](../index.md) / [Files](README.md) / Resolutions

File resolutions allow a single file record to have multiple resolution variants stored on disk without creating separate database rows per variant. This is different from the dimension system (`Dim`/`ParentId`) which creates separate `File` records for each variant.

---

## How It Works

- **One DB record, multiple physical files.** The `Res` JSON column on the `File` entity tracks which resolutions exist and their upload progress.
- **File naming:** Resolution files are stored as `{fileId}_{resolution}{extension}` (e.g., `abc123_600.jpg`, `abc123_100.jpg`).
- **Upload tracking:** During upload, the `Res` column stores size and progress. After completion, the entry is cleared to an empty object.

---

## Data Format

The `Res` column is a JSON dictionary. Keys are resolution values (smallest side in pixels), values are `ResolutionInfo` objects.

### During upload

```json
{
  "600": { "s": 120034, "u": 3004 },
  "100": { "s": 5200, "u": 5200 }
}
```

- `s` — total size in bytes
- `u` — uploaded bytes so far

### After upload completes

```json
{
  "600": {},
  "100": {}
}
```

Upload metadata (`s` and `u`) is cleared once upload is complete.

---

## API

### Upload a resolution file

#### Step 1: Create (POST)

```http
POST /api/v1/files/upload
Headers:
  Upload-Length: <resolution-file-size>
  Upload-Defer-Length: 1
  Upload-Metadata: id <base64(file-id)>, name <base64(photo.jpg)>, res <base64(600)>, size <base64(120034)>

Response: 201 Created
  Location: /api/v1/files/upload/<file-id>?res=600
```

**Behavior:**
- If the file does not exist: creates a new file record with `Res = { "600": { "s": 120034, "u": 0 } }`.
- If the file already exists: updates the `Res` column to add the new resolution entry. No new DB record is created.
- The `res` metadata key is an integer representing the resolution (smallest side in pixels).

#### Step 2: Upload chunks (PATCH)

```http
PATCH /api/v1/files/upload/<file-id>?res=600
Headers:
  Upload-Offset: <current-byte-offset>
Body: <binary-chunk>

Response: 204 No Content
  Upload-Offset: <new-byte-offset>
```

The `?res=600` query parameter (from the `Location` header returned by POST) tells the handler to write to the resolution file and update the resolution-specific progress in the `Res` column.

When the upload completes (`u >= s`), the resolution entry is cleared to `{}` and a `FileUploadedEvent` is published.

#### Step 3: Check upload status (HEAD)

```http
HEAD /api/v1/files/upload/<file-id>?res=600
Headers:
  Tus-Resumable: 1.0.0

Response: 200 OK
  Upload-Length: 120034
  Upload-Offset: 3004
```

When `?res` is provided, returns the resolution-specific upload progress instead of the main file's progress.

### Download a resolution file

```http
GET /api/v1/files/stream/<file-id>?res=600
```

Returns the resolution file stream. If the requested resolution does not exist in the file's `Res` dictionary, returns `400 Bad Request` with the message: `File with resolution 600 does not exist.`

### Delete a file (includes resolution files)

```http
DELETE /api/v1/files/upload/<file-id>

Response: 204 No Content
```

Deletes the original file, all dimension variants (`ParentId`-based), and all resolution files from storage.

---

## File Entity

The `Res` property on the `File` entity:

```csharp
[JsonObject]
public IDictionary<string, ResolutionInfo>? Res { get; set; }
```

The `ResolutionInfo` class:

```csharp
public class ResolutionInfo
{
    public long? S { get; set; }  // total size (null when complete)
    public long? U { get; set; }  // uploaded bytes (null when complete)
}
```

### FileResUpdate projection

For efficient Res-only updates, the `FileResUpdate` projection maps to the same `File` table:

```csharp
[Table(nameof(File))]
[MainEntity(typeof(File))]
public class FileResUpdate : IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }
    [JsonObject]
    public IDictionary<string, ResolutionInfo>? Res { get; set; }
}
```

---

## Storage Path

Resolution files are stored alongside the original file with a `_{res}` suffix:

| Original path | Resolution path (res=600) |
|---------------|--------------------------|
| `user1/project5/editors/abc123.jpg` | `user1/project5/editors/abc123_600.jpg` |
| `system/def456.png` | `system/def456_600.png` |
| `none/ghi789.pdf` | `none/ghi789_600.pdf` |

Note: Resolution paths use plain numbers (e.g., `_600`), not `_600px` like the dimension system.

---

## Resolution vs Dimension

| Feature | Resolution (`Res`) | Dimension (`Dim`/`ParentId`) |
|---------|-------------------|------------------------------|
| DB records | One record, JSON column | Separate record per variant |
| Physical files | `{id}_{res}.ext` | `{id}_{dim}px.ext` |
| Lookup | `?res=600` on stream endpoint | `?dim=200` on stream endpoint |
| Delete | Automatic with parent | Automatic with parent |
| Upload tracking | In `Res` JSON column | Separate `Uploaded` field per record |

---

## Example Workflow

1. **Upload original file** (no resolution):
   ```
   POST → creates file record with Id=abc123
   PATCH → uploads file content
   ```

2. **Add 600px resolution**:
   ```
   POST with metadata: id=abc123, name=photo.jpg, res=600, size=120034
   → File exists, updates Res column: { "600": { "s": 120034, "u": 0 } }
   → Returns Location: /api/v1/files/upload/abc123?res=600

   PATCH /api/v1/files/upload/abc123?res=600
   → Writes to abc123_600.jpg on disk
   → Updates Res: { "600": { "s": 120034, "u": 120034 } }
   → Upload complete → Res: { "600": {} }
   ```

3. **Add 100px resolution**:
   ```
   POST with metadata: id=abc123, name=photo.jpg, res=100, size=5200
   → Updates Res: { "600": {}, "100": { "s": 5200, "u": 0 } }
   ```

4. **Download 600px resolution**:
   ```
   GET /api/v1/files/stream/abc123?res=600
   → Returns abc123_600.jpg stream
   ```

5. **Delete file**:
   ```
   DELETE /api/v1/files/upload/abc123
   → Deletes abc123.jpg, abc123_600.jpg, abc123_100.jpg from storage
   → Deletes DB record
   ```

---

[Home](../../README.md) / [Docs](../index.md) / [Files](README.md) / **Resolutions**
