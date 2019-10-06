using System;
using System.Threading.Tasks;
using Android.Content;

namespace Android.App
{
    public class ServiceManager
    {
        public ServiceManager()
        {
            Connection = new SencillaServiceConnection();
        }

        public SencillaServiceConnection Connection { get; private set; }

        public void StartService(
            Action<SencillaService> onServiceConnected,
            Action<SencillaService> onServiceDisconnected)
        {
            var task = new Task(() =>
            {
                // 
                Connection.OnServiceConnectedEvent += onServiceConnected;
                Connection.OnServiceDisconnectedEvent += onServiceDisconnected;

                // Start our main service if it is not started 
                var startLimeSrv = new Intent(Application.Context, typeof(SencillaService));
                Application.Context.StartService(startLimeSrv);

                // Bind to lime service 
                var bindLimeSrv = new Intent(Application.Context, typeof(SencillaService));
                Application.Context.BindService(bindLimeSrv, Connection, Bind.AutoCreate);

            });
            task.Start();
        }

        public void Unbind()
        {
            Application.Context.UnbindService(Connection);
        }
    }
}