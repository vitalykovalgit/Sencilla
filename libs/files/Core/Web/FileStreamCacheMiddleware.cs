namespace Sencilla.Component.Files;

/// <summary>
/// Settings for <see cref="FileStreamCacheMiddleware"/>. Bound from <c>SencillaFiles:StreamCache</c>
/// by <c>o.AddStreamCache(configuration)</c>, alongside the storage providers.
/// </summary>
[DisableInjection]
public class FileStreamCacheOptions
{
    /// <summary>Config section, under the <see cref="SencillaFilesOptions"/> root — as the storages do.</summary>
    public const string Section = "StreamCache";

    /// <summary>Route prefix this applies to. Everything under it is treated as content-addressed.</summary>
    public string PathPrefix { get; set; } = "/api/v1/files/stream";

    /// <summary>Freshness lifetime, seconds. Default one year — the ceiling HTTP allows.</summary>
    public int MaxAgeSeconds { get; set; } = 31_536_000;

    /// <summary>
    /// Emit <c>immutable</c>. Correct only while a given <c>{id}?res={n}</c> never changes bytes —
    /// true when derivatives are written once at upload and a new size is a new <c>res</c> key rather
    /// than a rewrite. Turn this off if derivatives are ever REGENERATED in place, or clients will
    /// hold a stale copy for <see cref="MaxAgeSeconds"/> with no way to revalidate.
    /// </summary>
    public bool Immutable { get; set; } = true;
}

/// <summary>
/// Makes <c>GET /api/v1/files/stream/{id}?res={n}</c> properly cacheable.
///
/// <see cref="FileStreamController"/> sends <c>Cache-Control: public,max-age=172000</c> and nothing
/// else — no <c>ETag</c>, no <c>Last-Modified</c>. So there is no way to revalidate: once those ~2
/// days lapse, or the browser evicts the entry (easy, at ~700 KB each), the full body is fetched
/// again. It also stamps that same public header on 4xx, so a "resolution does not exist" 400 is
/// cached publicly for 2 days.
///
/// This adds a strong <c>ETag</c> derived from the identity that determines the bytes — the file id
/// plus the requested resolution — answers <c>If-None-Match</c> with a 304 BEFORE the pipeline runs
/// (so a revalidation costs no DB lookup and no blob read at all), and strips caching from error
/// responses.
/// </summary>
[DisableInjection]
public sealed class FileStreamCacheMiddleware(RequestDelegate next, IOptions<FileStreamCacheOptions> options)
{
    private readonly FileStreamCacheOptions settings = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsCacheableStreamRequest(context.Request))
        {
            await next(context);
            return;
        }

        var etag = BuildETag(context.Request);

        // Revalidation hit — answer before the endpoint touches the database or blob storage.
        if (IfNoneMatchContains(context.Request, etag))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            context.Response.Headers.ETag = etag;
            context.Response.Headers.CacheControl = CacheControlValue();
            VaryByOrigin(context);
            return;
        }

        // Runs after the endpoint has set its own headers but before they are flushed, so this wins.
        context.Response.OnStarting(() =>
        {
            // ONLY a 2xx carries the file's bytes, so only a 2xx may be cached as the file.
            // Anything else describes the CLIENT's situation, not the content: a 400 "resolution does
            // not exist", a 404, or — the one that bites — a 302 auth challenge to /login. Caching a
            // redirect for a year under the image's URL would wedge that image permanently, and no
            // reload would clear it. Assignment replaces the endpoint's own `public` freshness.
            if (!IsCacheableStatus(context.Response.StatusCode))
            {
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            }

            context.Response.Headers.ETag = etag;
            context.Response.Headers.CacheControl = CacheControlValue();
            VaryByOrigin(context);

            return Task.CompletedTask;
        });

        await next(context);
    }

    /// <summary>
    /// Declare that this body's CORS headers depend on <c>Origin</c>. The CORS middleware adds the
    /// Vary itself, but ONLY when it applies a policy — i.e. never for a request that carried no
    /// Origin (a plain <c>&lt;img src&gt;</c> with no <c>crossorigin</c>). That response has no
    /// Access-Control-Allow-Origin, and without a Vary the browser stores it as the ONE copy of this
    /// URL for a year and replays it for the crossorigin loaders, which then fail the CORS check
    /// against it. Cheap to state here, and it covers every caller of the stream endpoint.
    /// </summary>
    private static void VaryByOrigin(HttpContext context)
    {
        if (!StringValues.IsNullOrEmpty(context.Request.Headers.Origin)) return; // CORS middleware already did

        context.Response.Headers.Append("Vary", "Origin");
    }

    private bool IsCacheableStreamRequest(HttpRequest request)
        => HttpMethods.IsGet(request.Method) && request.Path.StartsWithSegments(settings.PathPrefix);

    private string BuildETag(HttpRequest request)
        => BuildETag(request.Path.Value, request.Query["res"].ToString());

    private static bool IfNoneMatchContains(HttpRequest request, string etag)
        => IfNoneMatchContains(request.Headers.IfNoneMatch, etag);

    private string CacheControlValue()
        => settings.Immutable
            ? $"public, max-age={settings.MaxAgeSeconds}, immutable"
            : $"public, max-age={settings.MaxAgeSeconds}";

    /// <summary>
    /// Whether a response may be cached AS the file. Only a 2xx carries its bytes — everything else
    /// describes the client's situation, not the content.
    /// </summary>
    public static bool IsCacheableStatus(int statusCode) => statusCode is >= 200 and < 300;

    /// <summary>
    /// Strong ETag over what actually determines the bytes: the file id (the trailing path segment)
    /// and the requested resolution. Quoted per RFC 9110.
    /// </summary>
    public static string BuildETag(string? path, string? res)
    {
        var id = path?[(path.LastIndexOf('/') + 1)..] ?? string.Empty;

        return $"\"{id}-{(string.IsNullOrEmpty(res) ? "full" : res)}\"";
    }

    /// <summary>True when the client already holds this exact entity (or sent <c>*</c>).</summary>
    public static bool IfNoneMatchContains(StringValues header, string etag)
    {
        if (header.Count == 0) return false;

        foreach (var value in header)
        {
            if (string.IsNullOrEmpty(value)) continue;
            if (value == "*") return true;

            foreach (var candidate in value.Split(','))
            {
                // Tolerate the weak prefix a proxy may add — the comparison is still on the same entity.
                var trimmed = candidate.Trim();
                if (trimmed.StartsWith("W/", StringComparison.Ordinal)) trimmed = trimmed[2..];

                if (trimmed == etag) return true;
            }
        }

        return false;
    }
}
