
namespace Sencilla.Component.Users;

/// <summary>
/// Audit attribution: who last modified the row. Stamped from the current user in
/// the write pipeline (see TrackStampHandler) and never client-writable on the
/// update path. Null for anonymous or system writes. Known limit: updates are
/// full-row overwrites, so a sibling CreatedBy column is only tamper-proof once
/// partial updates exist. Lives with the Users component alongside the stamping handler.
/// </summary>
public interface IEntityUpdatedByTrack : IEntityUpdateable
{
    Guid? UpdatedBy { get; set; }
}
