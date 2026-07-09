namespace Sencilla.Component.Audit;

/// <summary>What kind of change an audit row records.</summary>
public enum AuditAction : byte
{
    Insert = 1,
    Update = 2,
    Delete = 3,
}
