using System;
using System.IO;

namespace Sencilla.Core.Component
{
    public static class ComponentResourceEx
    {
        public static Stream GetResourceFile(this IComponent component, string fileName)
        {
            //return component?.GetType().GetResourceFile(fileName);
            throw new NotImplementedException();
        }
    }
}
