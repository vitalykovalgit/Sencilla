using System;
using Android.App;

namespace Android.Content
{
    public static class ContextEx
    {
        public static TType Resolve<TType>(this Context context)
        {
            if (context == null)
                throw new ArgumentNullException();

            var app = context as IApplication;
            if (app == null)
                throw new ArgumentNullException($"Parameter [{nameof(context)}] is not an IApplication");

            return app.R<TType>();
        }
    }
}