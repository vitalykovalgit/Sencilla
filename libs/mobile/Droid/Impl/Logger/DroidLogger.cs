
using System;
using Android.Util;
using Sencilla.Core.Logging;

namespace Sencilla.Mobile.Xamarin.Droid.Impl.Logger
{
    public class DroidLogger : ILogger
    {
        const string Tag = nameof(DroidLogger);

        public void Debug(string msg)
        {
            Log.Debug(Tag, msg);
        }

        public void Error(string msg)
        {
            Log.Error(Tag, msg);
        }

        public void Error(Exception ex)
        {
            Log.Error(Tag, ExpToString(ex));
        }

        public void Error(string msg, Exception ex)
        {
            Log.Error(Tag, $"{msg} \r\n {ExpToString(ex)}");
        }

        public void Fatal(string msg)
        {
            Error(msg);
        }

        public void Fatal(Exception ex)
        {
            Error(ex);
        }

        public void Info(string msg)
        {
            Log.Info(Tag, msg);
        }

        public void Warn(string msg)
        {
            Log.Warn(Tag, msg);
        }

        public void Warn(Exception ex)
        {
            Log.Warn(Tag, ExpToString(ex));
        }

        string ExpToString(Exception ex)
        {
            return $"{ex?.Message ?? ""}\r\n{ex?.StackTrace ?? ""}";
        }
    }
}