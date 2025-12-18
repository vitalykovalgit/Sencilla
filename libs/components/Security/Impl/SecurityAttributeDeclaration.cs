
namespace Sencilla.Component.Security;

public class SecurityAttributeDeclaration : ISecurityDeclaration
{
    SecurityAttributeDiscoverer Discoverer;

    public SecurityAttributeDeclaration(SecurityAttributeDiscoverer discoverer)
    {
        Discoverer = discoverer;
    }

    public Task<IEnumerable<Matrix>> Permissions(CancellationToken token) => Task.FromResult((IEnumerable<Matrix>)Discoverer.Permissions);
}



