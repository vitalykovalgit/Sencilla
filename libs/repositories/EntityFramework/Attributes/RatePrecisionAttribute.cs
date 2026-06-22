namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Exchange-rate precision (19, 9) for decimal rate columns. Rates need more fractional digits
/// than money (a rate can be a very small multiplier), so they get their own precision distinct
/// from <see cref="MoneyPrecisionAttribute"/>.
/// </summary>
public class RatePrecisionAttribute() : PrecisionAttribute(19, 9) 
{
}
