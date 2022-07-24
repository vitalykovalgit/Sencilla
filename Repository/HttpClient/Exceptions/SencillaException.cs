
namespace Sencilla.Core
{
    /// <summary>
    /// Base exception for all sencilla exceptions 
    /// </summary>
    public class SencillaException : System.Exception
    {
        public SencillaException()
        {
        }

        public SencillaException(string message)
            : base(message)
        {
        }
    }
}
