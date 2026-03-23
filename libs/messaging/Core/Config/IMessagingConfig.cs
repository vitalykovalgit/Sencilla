namespace Sencilla.Messaging;

/// <summary>
/// Interface for class-based messaging configuration.
/// Implementations are discovered via assembly scanning and applied during startup.
/// Similar to Entity Framework's <c>IEntityTypeConfiguration&lt;T&gt;</c> pattern.
/// </summary>
/// <example>
/// <code>
/// class MyStreamConfig : IMessagingConfig
/// {
///     public void Configure(MessagingConfig options)
///     {
///         options.Route(r => r.Message&lt;MyCommand&gt;().To("my-queue"));
///         options.AddConsumerFor("my-queue", c => {
///             c.HandleOnly&lt;MyCommand&gt;();
///         });
///     }
/// }
/// </code>
/// </example>
public interface IMessagingConfig
{
    /// <summary>
    /// Configures the messaging pipeline.
    /// </summary>
    /// <param name="options">The messaging configuration to modify.</param>
    void Configure(MessagingConfig options);
}
