namespace Sencilla.Component.Files;

/// <summary>
/// Projection mapped to the File table for efficient Res-only updates.
/// </summary>
[Table(nameof(File))]
[MainEntity(typeof(File))]
public class FileResUpdate : IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }

    /// <summary>
    /// Resolution variants (see <see cref="File.Res"/>)
    /// </summary>
    [JsonObject]
    public IDictionary<string, ResolutionInfo>? Res { get; set; }
}
