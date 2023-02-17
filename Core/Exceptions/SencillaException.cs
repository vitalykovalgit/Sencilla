
namespace Sencilla.Core;

/// <summary>
/// Base exception for all sencilla exceptions 
/// </summary>
public class SencillaException : Exception
{
    public SencillaException(string? message = null) : base(message)
    {
    }
}
