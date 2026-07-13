namespace Sencilla.Component.Users;

/// <summary>
/// Built in roles. Root/Anonymous/User are claims-derived and global-only; every
/// other role is holdable globally (sec.UserRole) or on a row ([SecureWith] role
/// table). Owner ⊃ Editor ⊃ Viewer via the seeded Parent chain.
/// </summary>
public enum RoleType
{
    Root  = 1,
    Anonymous = 2,
    User  = 3,
    Guest = 4,
    Admin = 5,

    /// <summary>Default creator role for [SecureWith] terminal entities; inherits Editor.</summary>
    Owner = 6,

    /// <summary>Write access on rows it is held on; inherits Viewer.</summary>
    Editor = 7,

    /// <summary>Read access on rows it is held on.</summary>
    Viewer = 8,
}
