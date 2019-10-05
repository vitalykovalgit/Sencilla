
namespace Sencilla.Core.Exceptions
{
    public class UnauthorizedException : SencillaException
    {
        public UnauthorizedException()
        {
        }

        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}
