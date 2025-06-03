using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Sencilla.Core.Logging;

namespace Android.App
{
    /// <summary>
    /// 
    /// </summary>
    [Service]
    public class SencillaService : Service
    {
        public string Tag = nameof(SencillaService);

        public ILogger Logger;
        public IBinder mBinder;
        public IService[] mServices;

        /// <summary>
        /// Not used here 
        /// </summary>
        public override IBinder OnBind(Intent intent)
        {
            mBinder = new SencillaServiceBinder(this);

            // ApplicationContext

            return mBinder;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Logger.Debug("SencillaService::OnCreate() is called...");

            Logger = ApplicationContext.R<ILogger>();
            mServices = ApplicationContext.R<IService[]>();

            // start login
            //Authorizer.LoginService(OnUserLoggedIn, OnUserLoggedOut);
        }

        //private void OnUserLoggedIn(IAuthorizeService srv)
        //{
        //    // Start all services
        //    foreach (var s in mServices)
        //    {
        //        try
        //        {
        //            s.Create();
        //            s.Start();
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error(Tag, $"Service {s} failed to start!");
        //            Logger.Error(Tag, ex);
        //        }
        //    }
        //}

        //private void OnUserLoggedOut(IAuthorizeService srv)
        //{
        //    // Do destroy here 
        //    foreach (var service in mServices)
        //    {
        //        service.Stop();
        //        service.Destroy();
        //    }
        //}

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            //Log.Debug(Tag, "LimeService::OnStartCommand() is called...");

            //return base.OnStartCommand(intent, flags, startId);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            //Log.Debug(Tag, "LimeService::OnDestroy() is called...");
            //OnUserLoggedOut(Authorizer);

            base.OnDestroy();
        }
    }
}