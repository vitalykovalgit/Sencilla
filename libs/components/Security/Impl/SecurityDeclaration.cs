
namespace Sencilla.Component.Security;

/// <summary>
/// Sample security 
/// </summary>
public abstract class SecurityDeclaration: ISecurityDeclaration
{
    protected List<Matrix> permissions = new();

    public Task<IEnumerable<Matrix>> Permissions(CancellationToken token)
    {
        DeclarePermissions();
        return Task.FromResult((IEnumerable<Matrix>)permissions);
    }

    public void Add(Matrix matrix) => permissions.Add(matrix);

    public void Allow(string role, string resource, Action action, string? constraint = null) 
        => Add(new Matrix { Role = role, Resource = resource, Action = (int)action, Constraint = constraint });

    public void Allow<TEntity>(string role, Action action, string? constraint = null)
        => Add(new Matrix { Role = role, Resource = SecurityProvider.ResourceName<TEntity>(), Action = (int)action, Constraint = constraint });


    public SecurityEntityBuilder Entity<TEntity>() => new SecurityEntityBuilder(this, SecurityProvider.ResourceName<TEntity>());
    public SecurityRoleBuilder Role(string role) => new SecurityRoleBuilder(this, role);

    public abstract void DeclarePermissions();
}

public class SecurityEntityBuilder
{
    string Resource;
    SecurityDeclaration Owner;

    public SecurityEntityBuilder(SecurityDeclaration owner, string resource)
    {
        Owner = owner;
        Resource = resource;
    }

    public SecurityEntityBuilder CanBeReadBy(string role, string? constraints = null) => CanDoAction(Action.Read, role, constraints);
    public SecurityEntityBuilder CanBeCreatedBy<TEntity>(string role, string? constraints = null) => CanDoAction(Action.Create, role, constraints);
    public SecurityEntityBuilder CanByUpdateBy<TEntity>(string role, string? constraints = null) => CanDoAction(Action.Update, role, constraints);
    public SecurityEntityBuilder CanByDeleteBy<TEntity>(string role, string? constraints = null) => CanDoAction(Action.Delete, role, constraints);

    protected SecurityEntityBuilder CanDoAction(Action action, string role, string? constraints = null)
    {
        Owner.Add(new Matrix
        {
            Role = role,
            Resource = Resource,
            Action = (int)action,
            Constraint = constraints
        });
        return this;
    }
}

public class SecurityRoleBuilder
{
    string Role;
    SecurityDeclaration Owner;

    public SecurityRoleBuilder(SecurityDeclaration owner, string role)
    {
        Owner = owner;
        Role = role;
    }

    public SecurityRoleBuilder CanRead<TEntity>(string? constraints = null) => Can<TEntity>(Action.Read, constraints);
    public SecurityRoleBuilder CanCreate<TEntity>(string? constraints = null) => Can<TEntity>(Action.Create, constraints);
    public SecurityRoleBuilder CanUpdate<TEntity>(string? constraints = null) => Can<TEntity>(Action.Update, constraints);
    public SecurityRoleBuilder CanDelete<TEntity>(string? constraints = null) => Can<TEntity>(Action.Delete, constraints);

    protected SecurityRoleBuilder Can<TEntity>(Action action, string? constraints = null)
    {
        Owner.Add(new Matrix
        {
            Role = Role,
            Resource = SecurityProvider.ResourceName<TEntity>(),
            Action = (int)action,
            Constraint = constraints
        });
        return this;
    }
}




