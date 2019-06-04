using System;
using Unity;
using Sencilla.Core.Injection;
using System.Collections.Generic;

namespace Sencilla.Impl.Injection.Unity
{
    class UnityResolver : IResolver
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

        public void RegisterType(Type iterface, Type implementation, string name)
        {
            mContainer.RegisterType(iterface, implementation, name);
        }

        public TType Resolve<TType>()
        {
            return mContainer.Resolve<TType>();
        }

        public object Resolve(Type type)
        {
            return mContainer.Resolve(type);
        }

        public TType Resolve<TType>(string name)
        {
            return mContainer.Resolve<TType>(name);
        }

        public IEnumerable<TType> ResolveAll<TType>()
        {
            return mContainer.ResolveAll<TType>();
        }
    }
}
