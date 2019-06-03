using System;
using Unity;
using Sencilla.Core.Injection;

namespace Sencilla.Impl.Injection.Unity
{
    public class UnityResolver : IResolver
    {
        IUnityContainer mContainer;

        public UnityResolver(IUnityContainer container)
        {
            mContainer = container;
        }

        public void RegisterType(Type iterface, Type implementation)
        {
            mContainer.RegisterType(iterface, implementation);
        }

        public TType Resolve<TType>()
        {
            return mContainer.Resolve<TType>();
        }

        public object Resolve(Type type)
        {
            return mContainer.Resolve(type);
        }
    }
}
