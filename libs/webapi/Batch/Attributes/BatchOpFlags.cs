namespace Sencilla.Web.Batch;

/// <summary>
/// Operations an entity opts into for the batch API. Combined as flags on
/// <see cref="SencillaEntityAttribute.Batch"/>.
/// </summary>
[Flags]
public enum BatchOpFlags
{
    None     = 0,
    Create   = 1 << 0,
    Update   = 1 << 1,
    Delete   = 1 << 2,
    Remove   = 1 << 3,
    Hide     = 1 << 4,
    Show     = 1 << 5,
    Upsert   = 1 << 6,
    GetAll   = 1 << 7,
    GetById  = 1 << 8,
    GetCount = 1 << 9,

    AllWrites = Create | Update | Delete | Remove | Hide | Show | Upsert,
    AllReads  = GetAll | GetById | GetCount,
    All       = AllWrites | AllReads,
}
