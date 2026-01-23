namespace Sencilla.Web.MinimalApi;

public class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("delete", () => "Delete endpoint");
    }
}