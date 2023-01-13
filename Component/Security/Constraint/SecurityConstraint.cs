using System.Reflection;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;

using Sencilla.Core;
using Sencilla.Component.Users;
using System.Collections.Concurrent;

namespace Sencilla.Component.Security
{
    public class ParsedConstraintCache
    {
        /// <summary>
        /// 
        /// </summary>
        private static ConcurrentDictionary<string, ParsedConstraint> Cache = new();

        public static ParsedConstraint Get(string constraint)
        {
            // 
            if (Cache.ContainsKey(constraint))
                return Cache[constraint];

            // 
            var parsed = new ParsedConstraint(constraint);
            Cache[constraint] = parsed;
            return parsed;
        }
    }

    public class ParsedConstraint
    {
        private List<string> Params = new();

        public ParsedConstraint(string constraint) 
        {
            Constraint = constraint;
            Parse(constraint);
        }

        public string Constraint { get; }

        public object[] Vars(ISystemVariable sysVars) => Params.Select(p => sysVars.Get(p)).ToArray();

        private void Parse(string constraint)
        {
            // replace all placeholders 
            var idx = 0;
            var replaced = Regex.Replace(constraint, "{[\\w\\s\\d.]*}", match =>
            {
                // add vars 
                var val = match.Value.Trim(' ', '{', '}');
                var var = new Regex("[\\s\\w\\d]*").Replace(val, m =>
                {
                    Params.Add(m.Value);
                    return $"${idx++}";
                }, 1);

                return var;
            });
        }
    }

    [Implement(typeof(IReadConstraint))]
    public class SecurityConstraint : Resolveable, IReadConstraint
    {
        ICurrentUserProvider UserProvider;

        public SecurityConstraint(ICurrentUserProvider currentUserProvider, IResolver resolver): base(resolver)
        { 
            UserProvider = currentUserProvider;
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
            using (var rootAccess = Access.Root())
            {
                // Get accesses 
                var matrix = await R<IReadRepository<Matrix>>().GetAll();
                var accesses = matrix.Where(m =>
                    (resource.Equals(m.Resource, StringComparison.OrdinalIgnoreCase)) &&
                    (action & m.Action) == action &&
                    (roles.Any(r => r.RoleId == m.Role))
                );

                // if operation is not allowed throw forbid exception 
                if (!accesses.Any())
                    throw new ForbiddenException();

                // Apply constraints 
                foreach (var a in accesses)
                {
                    if (!string.IsNullOrEmpty(a?.Constraint))
                    {
                        // 
                        var sysVars = R<ISystemVariable>();
                        var constr = ParsedConstraintCache.Get(a?.Constraint);
                        query = query.Where(a.Constraint, constr.Vars(sysVars));
                    }
                }
            }
            return query;
        }
    }
}
