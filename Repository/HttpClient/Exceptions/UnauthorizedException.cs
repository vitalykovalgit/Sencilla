
namespace Sencilla.Core
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
