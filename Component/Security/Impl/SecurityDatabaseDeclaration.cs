
namespace Sencilla.Component.Security;

public class SecurityDatabaseDeclaration: ISecurityDeclaration
{
    IReadRepository<Matrix> MatrixRepo;

    public SecurityDatabaseDeclaration(IReadRepository<Matrix> matrixRepo)
    {
        MatrixRepo = matrixRepo;
    }

    public IEnumerable<Matrix> Permissions() => AsyncHelper.RunSync(() => MatrixRepo.GetAll());
}


