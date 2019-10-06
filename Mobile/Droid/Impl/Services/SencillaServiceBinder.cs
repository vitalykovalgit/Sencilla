using Android.OS;

namespace Android.App
{
    /// <summary>
    /// Binder for service 
    /// </summary>
    public class SencillaServiceBinder : Binder
    {
        /// <summary>
        /// Construct binder for service 
        /// </summary>
        /// <param name="service"> service </param>
        public SencillaServiceBinder(SencillaService service)
        {
            Service = service;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsBound { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SencillaService Service { get; protected set; }

    }
}