
namespace Sencilla.Component.Security;

public class SecurityAttributeDeclaration(SecurityAttributeDiscoverer discoverer) : ISecurityDeclaration
{
    public Task<IEnumerable<Matrix>> Permissions(CancellationToken token) => Task.FromResult((IEnumerable<Matrix>)discoverer.Permissions);
}



