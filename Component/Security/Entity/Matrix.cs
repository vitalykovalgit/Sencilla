namespace Sencilla.Component.Security;

[Table(nameof(Matrix), Schema = "sec")]
public class Matrix : IEntity
{
    public int Id { get; set; }
    
    [Column("Role")]
    public int RoleId { get; set; }

    [NotMapped]
    public string Role { get; set; }

    public string? Resource { get; set; }
    
    public int Action { get; set; }

    public string? Constraint { get; set; }
}

/// <summary>
/// Matrix Extensions 
/// </summary>
public static class MatrixEx
{

    public static string Constraint(this Matrix? matrix) => matrix?.Constraint ?? "1 = 1";
}
