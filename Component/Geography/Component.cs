using Sencilla.Core;

namespace Sencilla.Component.Geography
{
    public class GeographyComponent : IComponent
    {
        public string Type => nameof(GeographyComponent);

        public void Init(IRegistrator container)
        {
        }
    }
}
