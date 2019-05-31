
namespace Sencilla.Core.Logging
{
    /// <summary>
    /// Logger 
    /// </summary>
    public interface ILogger
    {
        void Fatal(string msg);
        void Fatal(System.Exception ex);

        void Error(string msg);
        void Error(System.Exception ex);
        void Error(string msg, System.Exception ex);

        void Warn(string msg);
        void Warn(System.Exception ex);

        void Info(string msg);

        void Debug(string msg);
    }
}
