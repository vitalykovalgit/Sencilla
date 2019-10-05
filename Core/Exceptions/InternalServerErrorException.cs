
namespace Sencilla.Core.Exceptions
{
    public class InternalServerErrorException : SencillaException
    {
        public InternalServerErrorException()
        {
        }

        public InternalServerErrorException(string message)
            : base(message)
        {
        }
    }
}
