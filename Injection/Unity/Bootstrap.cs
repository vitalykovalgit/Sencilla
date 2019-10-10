using Unity;
using Sencilla.Impl.Injection.Unity;

namespace Sencilla.Core.Injection
{
    public static class Bootstrap
    {
        /// <summary>
        /// Retrive new instance of IResolver based on unity container 
        /// </summary>
        /// <returns></returns>
        public static IResolver Instance(this IResolver notUsed)
        {
            var contatiner = new UnityContainer();
            contatiner.RegisterSingleton<IResolver, UnityResolver>();

            return contatiner.Resolve<IResolver>();
        }
    }
}
