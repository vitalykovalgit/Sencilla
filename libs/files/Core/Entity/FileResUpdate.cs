namespace Sencilla.Component.Files;

/// <summary>
/// Projection mapped to the File table for efficient Res-only updates.
/// </summary>
[MainEntity(typeof(File))]
[Table(nameof(File))]
public class FileResUpdate : IEntity<Guid>, IEntityUpdateable
{
    public Guid Id { get; set; }
    /// <summary>
    /// Resolution variants (see <see cref="File.Res"/>)
    /// </summary>
    [JsonObject]
    [Column("Res")]
    public IDictionary<string, ResolutionInfo>? Res { get; set; }
}
