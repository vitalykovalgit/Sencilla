namespace Sencilla.Component.Security;

/// <summary>
/// Role-graph queries backed by a cached snapshot of sec.Role (raw read — no
/// entity events, so no recursion into security checks). Evicted by
/// <see cref="SecurityCacheSignal"/> on any Role change; short TTL backstop.
/// Contract in <see cref="IRoleClosure"/>.
/// </summary>
public class RoleClosure(IReadRepository<Role, int> roles, IMemoryCache cache, SecurityCacheSignal signal) : IRoleClosure
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private const string CacheKey = "sec_role_graph";

    private record Snapshot(Dictionary<int, int?> Parents, Dictionary<int, HashSet<int>> Grantors);

    private Snapshot Graph =>
        cache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
            entry.AddExpirationToken(signal.Token);
            // Raw query — framework-internal read, no events, no recursion.
            var parents = roles.Query.ToList().ToDictionary(r => r.Id, r => r.ParentId);

            // Reverse closure: role r grants everything its ancestors grant, so r is
            // a grantor of itself and of every ancestor on its parent chain.
            var grantors = new Dictionary<int, HashSet<int>>();
            foreach (var roleId in parents.Keys)
            {
                var visited = new HashSet<int>();
                var current = (int?)roleId;
                while (current != null && visited.Add(current.Value))
                {
                    (grantors.TryGetValue(current.Value, out var set)
                        ? set
                        : grantors[current.Value] = []).Add(roleId);
                    current = parents.TryGetValue(current.Value, out var p) ? p : null;
                }
            }

            return new Snapshot(parents, grantors);
        })!;

    public HashSet<int> Expand(HashSet<int> roleIds)
    {
        var graph = Graph.Parents;
        var result = new HashSet<int>();

        foreach (var roleId in roleIds)
        {
            // Walk the parent chain; the visited set tolerates pre-existing cycles.
            var current = (int?)roleId;
            while (current != null && result.Add(current.Value))
                current = graph.TryGetValue(current.Value, out var p) ? p : null;
        }

        return result;
    }

    public HashSet<int> GrantorsOf(int roleId) =>
        Graph.Grantors.TryGetValue(roleId, out var set) ? set : [roleId];

    public bool Exists(int roleId) => Graph.Parents.ContainsKey(roleId);

    public bool WouldCreateCycle(int roleId, int? parentId)
    {
        var graph = Graph.Parents;
        var visited = new HashSet<int>();
        var current = parentId;
        while (current != null && visited.Add(current.Value))
        {
            if (current.Value == roleId)
                return true;

            current = graph.TryGetValue(current.Value, out var p) ? p : null;
        }

        return false;
    }
}
