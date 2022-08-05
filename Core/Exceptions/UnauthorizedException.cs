
namespace Sencilla.Core
{
    public class UnauthorizedException : SencillaException
    {
        public UnauthorizedException(string? message = null): base(message)
        {
        }
    }
}
