using System;
using Android.Content;
using Android.OS;

namespace Android.App
{
    /// <summary>
    /// 
    /// </summary>
    public class SencillaServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public SencillaServiceBinder Binder { get; private set; }

        public event Action<SencillaService> OnServiceConnectedEvent = delegate { };
        public event Action<SencillaService> OnServiceDisconnectedEvent = delegate { };

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var binder = service as SencillaServiceBinder;
            if (binder != null)
            {
                Binder = binder;
                Binder.IsBound = true;

                OnServiceConnectedEvent(binder.Service);
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            if (Binder != null)
            {
                OnServiceDisconnectedEvent(Binder.Service);
                Binder.IsBound = false;
                Binder = null;
            }
        }
    }
}