
namespace Sencilla.Component.Security;

/// <summary>
/// Entity operations a permission grants. Bit flags so a single Matrix row can
/// grant combinations (e.g. Read | Update). Values are persisted in
/// [sec].[Matrix].[Action] and seeded in [sec].[Action]; changing them requires
/// a data migration (see Database/Data/MigrateActionFlags.sql).
/// </summary>
[Flags]
public enum Action
{
    Read   = 1,
    Create = 2,
    Update = 4,
    Delete = 8,

    All = Read | Create | Update | Delete,
}
