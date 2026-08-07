namespace Sencilla.Component.Security;

/// <summary>
/// Authorization for resources that are NOT entities — pages, screens, operations, anything a
/// <c>[CrudApi]</c> type does not cover.
///
/// The entity path (<see cref="SecurityConstraintHandler{TEntity}"/>) composes permissions INTO a
/// query, so it can express row scoping and needs an <c>IQueryable</c> to narrow. An endpoint that
/// performs an operation has no such query: it needs a yes/no before it runs. Both read the same
/// <c>sec.Matrix</c> rows through the same <see cref="IUserRoleResolver"/>, so the two answers cannot
/// drift.
///
/// <para><b>The verb lives in the resource, never in the action.</b> <c>sec.Action</c> is a closed,
/// FK-enforced set of the CRUD bit flags, so <c>Checkout</c> or <c>Publish</c> cannot be seeded:
/// name a resource for the operation instead.</para>
///
/// <code>
/// // (Manager, 'admin.orders.payment', Update, NULL) absent → 403
/// app.MapPost("api/v1/orders/{id}/pay", Pay).RequirePermission("admin.orders.payment", Action.Update);
/// </code>
/// </summary>
public interface IPermissionChecker
{
    /// <summary>True when the caller holds a grant for <paramref name="resource"/> covering every bit
    /// of <paramref name="action"/>. Fails CLOSED: no row means no.</summary>
    Task<bool> HasAsync(string resource, Action action, CancellationToken token);

    /// <summary>
    /// <see cref="HasAsync"/>, throwing <see cref="ForbiddenException"/> (→ 403) instead of returning
    /// false. The default for endpoints: a denied call must not reach the handler, so a forgotten
    /// <c>if</c> around a bool cannot silently permit the operation.
    /// </summary>
    Task DemandAsync(string resource, Action action, CancellationToken token);

    /// <summary>
    /// Every resource the caller is granted, mapped to the OR of its action bits — the payload the
    /// client needs to hide what it may not do.
    ///
    /// The client cannot compute this itself: role inheritance lives in <c>sec.Role.Parent</c> and the
    /// rows are not on the user payload at all, so a client-side fold would either re-implement the
    /// closure or silently ignore it. Resolved here, shipped flat.
    ///
    /// <see cref="AllResources"/> is present with <see cref="Action.All"/> when the caller is Root,
    /// which no enumeration of rows could otherwise represent.
    /// </summary>
    Task<Dictionary<string, int>> GrantedAsync(CancellationToken token);

    /// <summary>The wildcard key in <see cref="GrantedAsync"/>'s map: the caller may do anything.</summary>
    const string AllResources = "*";
}
