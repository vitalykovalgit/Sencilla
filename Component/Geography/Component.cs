using Sencilla.Core.Component;
using Sencilla.Core.Injection;

namespace Sencilla.Component.Geography
{
    public class GeographyComponent : IComponent
    {
        public string Type => nameof(GeographyComponent);

        public void Init(IResolver resolver)
        {
            
        }
    }
}
