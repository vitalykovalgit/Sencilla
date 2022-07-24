
namespace Sencilla.Core
{
    /// <summary>
    /// Logger 
    /// </summary>
    public interface ILogger
    {
        void Fatal(string msg);
        void Fatal(Exception ex);

        void Error(string msg);
        void Error(Exception ex);
        void Error(string msg, Exception ex);

        void Warn(string msg);
        void Warn(Exception ex);

        void Info(string msg);

        void Debug(string msg);
    }
}
