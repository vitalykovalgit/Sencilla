using System.Reflection;
using Sencilla.Component.Users;
using Sencilla.Core;

namespace Sencilla.Component.Security
{
    [Implement(typeof(IReadConstraint))]
    public class SecurityConstraint : Resolveable, IReadConstraint
    {
        ICurrentUserProvider UserProvider;
        //IReadRepository<Matrix> MatrixRepo;
        public SecurityConstraint(ICurrentUserProvider currentUserProvider, IResolver resolver): base(resolver)
        { 
            UserProvider = currentUserProvider;
            //MatrixRepo = resolver.Resolve<>;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ResourceName<TEntity>()
        {
            // try to get attribute 
            var entityType = typeof(TEntity);
            var resourceAttr = entityType.GetCustomAttribute<ResourceAttribute>();

            return resourceAttr?.Name ?? entityType.Name; 
        }

        public async Task<IQueryable<TEntity>> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter = null)
        {
            // Check if access allowed then do nothing 
            if (Access.Current?.AllowAll ?? false)
                return query;

            // current user
            var user  = UserProvider.CurrentUser; // get current user and his roles
            var roles = user.Roles;
            var action = (int)Action.Read;
            var resource = ResourceName<TEntity>(); // resource name or all

            // Allow root access 
            using var access = Access.Root();
            
            var matrix = await R<IReadRepository<Matrix>>().GetAll();

            var accesses = matrix.Where(m =>
                (resource.Equals(m.Resource, StringComparison.OrdinalIgnoreCase)) &&
                (action & m.Action) == action &&
                (roles.Any(r => r.RoleId == m.Role))
            );

            // if operation is not allowed throw forbid exception 
            if (!accesses.Any())
                throw new ForbiddenException();


            // read matrix with security constraints
            // {"State": { "in": [1, 2, 3, 4, 5] } } & {"UserId": { "in": [1, 2, 3, 4, 5] } }
            //foreach (var a in accesses)
            //{
            //    a.Constraints
            //}

            // apply constraints
            //query = query.Where(e => EF.Property<object>(e, "Name").Equals("Milan"));

            return query;
        }
    }

    //public class 

    public class Expression
    {
        public object? Eq { get; set; }
        
        public object? Ls { get; set; }
        
        public object? Mr { get; set; }

        public object? Lk { get; set; }

        public object[]? In { get; set; }
    }
}
