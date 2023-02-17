
namespace Sencilla.Core;

public class BadRequestException : SencillaException
{
    public BadRequestException(string? message = null) : base(message)
    {
    }
}
