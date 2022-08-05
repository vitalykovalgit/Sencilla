
namespace Sencilla.Core
{
    public class Resolveable
    {
        protected IResolver Resolver { get; }

        public Resolveable(IResolver resolver)
        {
            Resolver = resolver;
        }

        protected T? R<T>()
        {
            return Resolver.Resolve<T>();
        }
    }
}
