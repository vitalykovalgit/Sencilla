namespace Sencilla.Messaging.EntityFramework;

public class EfMessagingProviderConfig : ProviderConfig
{
    public EfMessagingOptions Options { get; } = new();

    public EfMessagingProviderConfig WithOptions(Action<EfMessagingOptions> config)
    {
        config(Options);
        return this;
    }
}
