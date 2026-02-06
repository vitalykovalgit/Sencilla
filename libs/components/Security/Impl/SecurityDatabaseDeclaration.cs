
namespace Sencilla.Component.Security;

public class SecurityDatabaseDeclaration(IReadRepository<Matrix> matrixRepo) : ISecurityDeclaration
{
    public Task<IEnumerable<Matrix>> Permissions(CancellationToken token) => matrixRepo.GetAll(null, token);
}


