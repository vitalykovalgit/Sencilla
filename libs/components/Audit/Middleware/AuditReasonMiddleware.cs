namespace Sencilla.Component.Audit;

/// <summary>
/// Reads the optional <c>X-Audit-Reason</c> request header into the per-request <see cref="IAuditContext"/>,
/// so a generic CRUD mutation can carry a reason without a bespoke endpoint. Chain after <c>UseSencillaUser</c>.
/// </summary>
[DisableInjection]
public class AuditReasonMiddleware(RequestDelegate next)
{
    const string ReasonHeader = "X-Audit-Reason";

    // audit.Audit.Reason is NVARCHAR(1024). Cap the client-supplied header so an oversized value can never
    // overflow the column mid-INSERT — that write runs inside the audited operation's transaction, so a
    // failure there would roll back the business change itself.
    const int MaxReasonLength = 1024;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(ReasonHeader, out var reason) && !string.IsNullOrWhiteSpace(reason))
        {
            var ctx = context.RequestServices.GetService<IAuditContext>();
            if (ctx != null)
            {
                // Header values are byte strings, so a non-ASCII reason (Cyrillic, emoji, …) cannot travel raw —
                // browsers reject it outright at setRequestHeader/Headers. Clients percent-encode; decode here.
                // Undecodable input is left as-is by UnescapeDataString, so a plain ASCII reason still works.
                var value = Uri.UnescapeDataString(reason.ToString());
                ctx.Reason = value.Length > MaxReasonLength ? value[..MaxReasonLength] : value;
            }
        }

        await next(context);
    }
}
