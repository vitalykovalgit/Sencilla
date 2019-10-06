using System;

using Android.Content;
using Android.Runtime;

using Sencilla.Core.Injection;
using Sencilla.Core.Logging;
using Sencilla.Mobile.Xamarin.Droid.Impl.Logger;

namespace Android.App
{
    public abstract class InjectableApplication : Application, IApplication
    {
        public InjectableApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            AppContext = this;

            // Init container 
            DI = DI.Instance();
            DI.RegisterInstance<IApplication>(this);
            DI.RegisterType<ILogger, DroidLogger>();
            InitContainer(DI);
        }

        /// <summary>
        /// Application context
        /// </summary>
        public Context AppContext { get; set; }

        /// <summary>
        /// Resolve provided type for instance 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        public TType R<TType>()
        {
            return DI.Resolve<TType>();
        }

        /// <summary>
        /// Container 
        /// </summary>
        protected IResolver DI { get; private set; }

        /// <summary>
        /// Init container 
        /// </summary>
        /// <param name="container"></param>
        protected abstract void InitContainer(IResolver container);
    }
}