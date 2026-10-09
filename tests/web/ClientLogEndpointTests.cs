namespace Sencilla.Web.Tests;

/// <summary>
/// <see cref="ClientLogEndpoint"/> takes anonymous, untrusted bodies straight into the server logs,
/// so its limits are the contract: 204 for a report, 400 for anything that is not one, 413 past 16 KB
/// whatever Content-Length says — and the report travels as parameters of ONE constant template, never
/// as the template itself. (The 120-a-minute window is shared per process and left to integration runs.)
/// </summary>
public class ClientLogEndpointTests
{
    private const string Template = "client-error {Kind} {Message} {Path} {Build} {WindowClass} {InputType} {UserAgent} {Extra} {Stack}";

    [Fact]
    public async Task AReport_IsLoggedAsParameters_Truncated()
    {
        var log = new Capture();
        var message = "{Path} " + new string('x', 600);

        var status = await Post($$$"""{"kind":"upload","message":"{{{message}}}","path":"/cabinet/projects/1","window_class":"compact","input_type":"touch","extra":{"photos":3}}""", log);

        Assert.Equal(StatusCodes.Status204NoContent, status);
        var (level, values) = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, level);
        Assert.Equal(Template, values["{OriginalFormat}"]);
        Assert.Equal(message[..500], values["Message"]);
        Assert.Equal("/cabinet/projects/1", values["Path"]);
        Assert.Equal("compact", values["WindowClass"]);
        Assert.Equal("touch", values["InputType"]);
        Assert.Equal("""{"photos":3}""", values["Extra"]);
        Assert.Equal("test-agent", values["UserAgent"]);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("[1,2]")]
    [InlineData("null")]
    [InlineData("""{"message":42}""")]
    public async Task NotAReport_Is400(string body)
        => Assert.Equal(StatusCodes.Status400BadRequest, await Post(body));

    [Theory]
    [InlineData(true)]
    [InlineData(false)] // chunked: no Content-Length, the read itself stops at the cap
    public async Task PastSixteenKilobytes_Is413(bool declared)
        => Assert.Equal(StatusCodes.Status413PayloadTooLarge, await Post($$"""{"message":"{{new string('x', 20 * 1024)}}"}""", declared: declared));

    [Fact]
    public void AddClientLogging_RegistersTheEndpointOnce()
    {
        var services = new ServiceCollection().AddClientLogging().AddClientLogging();

        Assert.IsType<ClientLogEndpoint>(Assert.Single(services.BuildServiceProvider().GetServices<IEndpoint>()));
    }

    private static async Task<int> Post(string body, Capture? log = null, bool declared = true)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var http = new DefaultHttpContext();
        http.Request.Body = new MemoryStream(bytes);
        http.Request.ContentLength = declared ? bytes.Length : null;
        http.Request.Headers.UserAgent = "test-agent";

        var result = await new ClientLogEndpoint().LogAsync(http, log ?? new Capture(), CancellationToken.None);
        return ((IStatusCodeHttpResult)result).StatusCode!.Value;
    }

    private sealed class Capture : ILogger<ClientLogEndpoint>
    {
        public List<(LogLevel Level, Dictionary<string, object?> Values)> Entries { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, (state as IEnumerable<KeyValuePair<string, object?>> ?? []).ToDictionary(p => p.Key, p => p.Value)));

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
