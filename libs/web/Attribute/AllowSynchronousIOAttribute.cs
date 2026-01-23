namespace Sencilla.Web;

/// <summary>
/// Enables synchronous IO operations for the decorated action method only
/// </summary>
public class AllowSynchronousIOAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var syncIOFeature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (syncIOFeature != null)
            syncIOFeature.AllowSynchronousIO = true;

        base.OnActionExecuting(context);
    }
}
