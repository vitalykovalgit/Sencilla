
using System;

namespace Sencilla.Core.Logging.Impl
{
    class NoLogger : ILogger
    {
        public void Debug(string msg)
        {
        }

        public void Error(string msg)
        {
        }

        public void Error(Exception ex)
        {
        }

        public void Error(string msg, Exception ex)
        {
        }

        public void Fatal(string msg)
        {
        }

        public void Fatal(Exception ex)
        {
        }

        public void Info(string msg)
        {
        }

        public void Warn(string msg)
        {
        }

        public void Warn(Exception ex)
        {
        }
    }
}
