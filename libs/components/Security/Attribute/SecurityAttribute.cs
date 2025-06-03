namespace Sencilla.Component.Security;

/// <summary>
/// Define custom resource for the entity
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AllowAccessAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public AllowAccessAttribute(string role, Action action, string? constraint = null)
    {
        Role = role;
        Action = action;
        Constraint = constraint;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Action Action { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Constraint { get; set; }

    //public Expression<Func<Matrix, bool>>? Exprs { get; set; }
}


public class AllowReadAttribute: AllowAccessAttribute
{
    public AllowReadAttribute(string role, string? constraint = null): base(role, Action.Read, constraint) 
    { 
    }
}

public class AllowCreateAttribute: AllowAccessAttribute
{
    public AllowCreateAttribute(string role, string? constraint = null) : base(role, Action.Create, constraint)
    {
    }
}

public class AllowUpdateAttribute: AllowAccessAttribute
{
    public AllowUpdateAttribute(string role, string? constraint = null) : base(role, Action.Update, constraint)
    {
    }
}

public class AllowDeleteAttribute: AllowAccessAttribute
{
    public AllowDeleteAttribute(string role, string? constraint = null) : base(role, Action.Delete, constraint)
    {
    }
}