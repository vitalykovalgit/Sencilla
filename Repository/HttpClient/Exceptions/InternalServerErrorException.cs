
namespace Sencilla.Core
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
