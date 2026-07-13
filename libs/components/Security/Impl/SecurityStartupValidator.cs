using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sencilla.Component.Security;

/// <summary>
/// One (resource name → entity type) mapping, registered per secured entity by the
/// security bootstrap and consumed by <see cref="SecurityStartupValidator"/>. A DI
/// singleton (not process-static) so each host validates against its OWN entities —
/// two hosts in one process (e.g. parallel test factories) never cross-contaminate.
/// </summary>
public sealed record SecurityResourceRegistration(string Name, Type EntityType);

/// <summary>
/// Fails the deploy, not the first request: at host start, every matrix constraint
/// string is parsed against its resource's entity type, so a typo in a permission
/// row (misspelled member, broken syntax) surfaces as a startup crash with the
/// offending rows named — never as a runtime 500 or a silently inert grant.
///
/// ([SecureWith] chains are validated even earlier — at type registration.)
///
/// Placeholder handling — constraints reference request-scoped variables that do not
/// exist at boot (<c>userId={user}.Id</c>). The validator supplies a representative
/// typed value for the conventional <c>{user}</c> variable so such constraints parse
/// fully (and their entity-member typos are still caught). A constraint that also
/// references some OTHER unresolvable variable cannot be safely type-checked, so a
/// parse failure there is logged as a warning rather than blocking the deploy — a
/// false deploy-block on a valid constraint is worse than a missed check.
///
/// Skips gracefully when the permission source is unreachable (fresh env, DB
/// migrating) and when a resource has no entity type (pure-string resources, e.g. UI
/// views). Registered once by the bootstrap (see the [DisableInjection] below).
/// </summary>
[DisableInjection] // registered explicitly by the bootstrap; keep auto-discovery from double-registering
public class SecurityStartupValidator(IServiceProvider services, IEnumerable<SecurityResourceRegistration> resources) : IHostedService
{
    public async Task StartAsync(CancellationToken token)
    {
        var byResource = resources
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().EntityType, StringComparer.OrdinalIgnoreCase);

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetService<ILogger<SecurityStartupValidator>>();

        IEnumerable<Matrix> rows;
        try
        {
            using (Access.Root())
                rows = (await provider.GetRequiredService<ISecurityProvider>().GetAllPermissions(token)).ToList();
        }
        catch (Exception ex)
        {
            // Permission sources may legitimately be unavailable at boot (fresh
            // environment, DB migrating). Do not block startup on infrastructure —
            // only on provably broken configuration.
            logger?.LogWarning(ex, "Matrix constraint validation skipped: permission sources unavailable at startup.");
            return;
        }

        var failures = new List<string>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Constraint) || string.IsNullOrWhiteSpace(row.Resource))
                continue;

            if (!byResource.TryGetValue(row.Resource, out var entityType))
                continue; // pure-string resource (e.g. a UI view) — nothing to type-check against

            var parsed = ParsedConstraintCache.Get(row.Constraint);

            // Representative typed values: {user} -> a User instance (its members type
            // correctly, e.g. {user}.Id is Guid). Any other placeholder is unknown at
            // boot -> null, which makes a valid constraint fail to parse; those rows
            // are warned, not failed.
            var values = parsed.Parameters
                .Select(p => string.Equals(p, "user", StringComparison.OrdinalIgnoreCase) ? new User() : (object?)null)
                .ToArray();
            var hasUntypedPlaceholder = values.Any(v => v == null);

            try
            {
                DynamicExpressionParser.ParseLambda(entityType, typeof(bool), parsed.Constraint, values);
            }
            catch (Exception ex)
            {
                var where = $"(role {row.RoleId}, resource '{row.Resource}', action {row.Action}): \"{row.Constraint}\"";
                if (hasUntypedPlaceholder)
                    logger?.LogWarning("Permission constraint {Where} could not be fully validated at startup (references a request-scoped variable): {Error}", where, ex.Message);
                else
                    failures.Add($"{where} — {ex.Message}");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException(
                $"Invalid permission matrix constraints (deploy blocked):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
