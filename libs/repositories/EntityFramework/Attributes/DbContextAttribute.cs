namespace Sencilla.Repository.EntityFramework;

public interface IDbContextAttribute
{
    Type Type { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public class DbContextAttribute<T>: Attribute, IDbContextAttribute where T : DbContext
{
    public Type Type { get; } = typeof(T);
}
