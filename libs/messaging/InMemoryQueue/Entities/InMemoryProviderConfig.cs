namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryProviderConfig: ProviderConfig
{
    InMemoryProviderOptions Options = new();

    public InMemoryProviderConfig WithOptions(Action<InMemoryProviderOptions> config)
    {
        config(Options);
        return this;
    }   
}
