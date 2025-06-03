
namespace Sencilla.Component.Security;

public class SecurityAttributeDeclaration : ISecurityDeclaration
{
    SecurityAttributeDiscoverer Discoverer;

    public SecurityAttributeDeclaration(SecurityAttributeDiscoverer discoverer)
    {
        Discoverer = discoverer;
    }

    public IEnumerable<Matrix> Permissions() => Discoverer.Permissions;
}



