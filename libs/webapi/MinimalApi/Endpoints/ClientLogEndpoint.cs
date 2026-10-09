namespace Sencilla.Web.MinimalApi;

/// <summary>
/// <c>POST /api/v1/client-log</c>: a browser client reports an error it hit (an uncaught error, a failed
/// save or upload …) and the report becomes ONE structured warning in the server logs, wherever the host
/// ships them. No table. Opted into by <see cref="Bootstrap.AddClientLogging"/>, mapped by
/// <see cref="Bootstrap.MapSencillaEndpoints"/>.
///
/// Anonymous, so its content is untrusted and it can be spammed. The limits: a 16 KB body (413), JSON
/// or 400, and one fixed window of 120 reports a minute per process (429). Every field is truncated and goes
/// out as a logging PARAMETER only — never interpolated into the message template. Clients post
/// <c>text/plain</c> to stay clear of a CORS preflight, so the body is read raw; a signed-in caller's
/// cookie still reaches the host's own log enrichment.
///
/// The body, every field optional: <c>{ kind, message, stack, path, build, window_class, input_type, extra }</c>,
/// <c>extra</c> a small free-form object.
/// </summary>
public class ClientLogEndpoint : IEndpoint
{
    // ponytail: route and limits are constants; take options when a host needs different ones.
    private const int MaxBody = 16 * 1024;

    /// <summary>Per process, shared by every caller: the cap on what one spammer can write to the logs.</summary>
    private static readonly FixedWindowRateLimiter Limiter = new(new FixedWindowRateLimiterOptions
    {
        PermitLimit = 120,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    });

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/client-log", LogAsync)
           .WithName("ClientLog")
           .AllowAnonymous();
    }

    public async Task<IResult> LogAsync(HttpContext http, ILogger<ClientLogEndpoint> logger, CancellationToken token)
    {
        using var lease = Limiter.AttemptAcquire();
        if (!lease.IsAcquired)
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);

        if (http.Request.ContentLength > MaxBody)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        // At most one byte past the cap, whatever Content-Length claimed (a chunked body has none).
        var body = new byte[MaxBody + 1];
        var length = 0;
        int read;
        while (length < body.Length && (read = await http.Request.Body.ReadAsync(body.AsMemory(length), token)) > 0)
            length += read;
        if (length > MaxBody)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        ClientReport? report;
        try { report = JsonSerializer.Deserialize<ClientReport>(body.AsSpan(0, length), Json); }
        catch (JsonException) { return Results.BadRequest(); }
        if (report == null)
            return Results.BadRequest();

        logger.LogWarning("client-error {Kind} {Message} {Path} {Build} {WindowClass} {InputType} {UserAgent} {Extra} {Stack}",
            Cut(report.Kind, 20), Cut(report.Message, 500), Cut(report.Path, 500), Cut(report.Build, 200),
            Cut(report.WindowClass, 20), Cut(report.InputType, 20), Cut(http.Request.Headers.UserAgent.ToString(), 500),
            Cut(report.Extra?.GetRawText(), 1000), Cut(report.Stack, 4000));

        return Results.NoContent();
    }

    private static string? Cut(string? value, int max) => value?.Length > max ? value[..max] : value;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>The wire shape; every field is optional, <c>extra</c> a small free-form object.</summary>
    private sealed class ClientReport
    {
        public string? Kind { get; set; }
        public string? Message { get; set; }
        public string? Stack { get; set; }
        public string? Path { get; set; }
        public string? Build { get; set; }

        [JsonPropertyName("window_class")]
        public string? WindowClass { get; set; }

        [JsonPropertyName("input_type")]
        public string? InputType { get; set; }

        public JsonElement? Extra { get; set; }
    }
}
