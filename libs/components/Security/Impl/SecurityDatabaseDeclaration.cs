
namespace Sencilla.Component.Security;

public class SecurityDatabaseDeclaration: ISecurityDeclaration
{
    IReadRepository<Matrix> MatrixRepo;

    public SecurityDatabaseDeclaration(IReadRepository<Matrix> matrixRepo)
    {
        MatrixRepo = matrixRepo;
    }

    public Task<IEnumerable<Matrix>> Permissions(CancellationToken token) => MatrixRepo.GetAll(null, token);
}


