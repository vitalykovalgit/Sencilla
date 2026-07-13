
namespace Sencilla.Component.Users;

/// <summary>
/// Audit attribution: who created the row. Stamped from the current user in the
/// write pipeline (see TrackStampHandler) and never client-writable — any incoming
/// value is overwritten. Null for anonymous or system writes. Gives every opted-in
/// entity a constraint target ('CreatedBy == {user.id}') without an app-specific
/// owner column. Lives with the Users component because attribution is a user concept
/// and the stamping handler lives here.
/// </summary>
public interface IEntityCreatedByTrack : IEntityCreateable
{
    Guid? CreatedBy { get; set; }
}
