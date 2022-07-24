
namespace Sencilla.Core
{
    public class BadRequestException : SencillaException
    {
        public BadRequestException()
            //: this(new ServerError())
        {
        }

        public BadRequestException(string message)
            : base(message)
        {
        }

        //public BadRequestException(ServerError serverError)
        //{
        //    ServerError = serverError;
        //}

        //public ServerError ServerError { get; }
    }
}
