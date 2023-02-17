
namespace Sencilla.Core;

public class ForbiddenException : SencillaException
{
    public ForbiddenException(string? message = null) : base(message)
    {
    }
}
