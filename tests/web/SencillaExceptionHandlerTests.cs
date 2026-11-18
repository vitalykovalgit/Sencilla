namespace Sencilla.Web.Tests;

public class SencillaExceptionHandlerTests
{
    class CustomValidationException(string? message = null) : BadRequestException(message);

    static readonly System.Security.Claims.ClaimsPrincipal SignedIn = new(new System.Security.Claims.ClaimsIdentity(authenticationType: "Cookies"));

    static async Task<(bool Handled, HttpContext Context, JsonDocument? Body)> Handle(Exception exception, System.Security.Claims.ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        if (user != null)
            context.User = user;
        context.Response.Body = new MemoryStream();

        var handled = await new SencillaExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (handled, context, body.Length > 0 ? JsonDocument.Parse(body) : null);
    }

    [Theory]
    [InlineData(typeof(BadRequestException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(UnauthorizedException), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(ForbiddenException), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict)]
    [InlineData(typeof(InternalServerErrorException), StatusCodes.Status500InternalServerError)]
    [InlineData(typeof(SencillaException), StatusCodes.Status500InternalServerError)]
    public async Task Maps_Sencilla_Exceptions_To_Status_Codes_For_A_Signed_In_Caller(Type exceptionType, int expectedStatus)
    {
        var (handled, context, body) = await Handle((Exception)Activator.CreateInstance(exceptionType, [null])!, SignedIn);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus, body!.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData(typeof(ForbiddenException))]
    [InlineData(typeof(UnauthorizedException))]
    public async Task Refusal_Without_A_Session_Answers_401(Type exceptionType)
    {
        var (handled, context, body) = await Handle((Exception)Activator.CreateInstance(exceptionType, [null])!);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, body!.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Forbidden_For_A_Signed_In_Caller_Keeps_Its_Message_And_403_Title()
    {
        var (_, context, body) = await Handle(new ForbiddenException("not yours"), SignedIn);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("Forbidden", body!.RootElement.GetProperty("title").GetString());
        Assert.Equal("not yours", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Derived_Exception_Maps_Via_Its_Base()
    {
        var (handled, context, _) = await Handle(new CustomValidationException("amount must be positive"));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Message_Becomes_ProblemDetails_Detail()
    {
        var (_, _, body) = await Handle(new BadRequestException("amount must be positive"));

        Assert.Equal("amount must be positive", body!.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Messageless_Exception_Omits_Detail()
    {
        var (_, _, body) = await Handle(new ForbiddenException());

        Assert.False(body!.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task Writes_ProblemDetails_Content_Type()
    {
        var (_, context, _) = await Handle(new BadRequestException("nope"));

        Assert.StartsWith("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task Ignores_Non_Sencilla_Exceptions()
    {
        var (handled, context, body) = await Handle(new InvalidOperationException("boom"));

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Null(body);
    }
}
