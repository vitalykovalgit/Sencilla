
namespace Sencilla.Core.Exceptions
{
    public class ForbiddenException : SencillaException
    {
        public ForbiddenException()
            //: this(new ServerError())
        {
        }

        public ForbiddenException(string message)
            : base(message)
        {
        }

        //public ForbiddenException(ServerError serverError)
        //{
        //    ServerError = serverError;
        //}

        //public ServerError ServerError { get; }
    }
}
