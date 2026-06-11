namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Standard money precision (18, 4) for decimal columns.
/// </summary>
public class MoneyPrecisionAttribute : PrecisionAttribute
{
    public MoneyPrecisionAttribute() : base(18, 4)
    {
    }
}
