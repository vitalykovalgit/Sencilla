namespace Sencilla.Messaging.SignalR;

[DisableInjection]
public class SignalRProviderConfig : ProviderConfig
{
    SignalRProviderOptions Options = new();

    public SignalRProviderConfig WithOptions(Action<SignalRProviderOptions> config)
    {
        config(Options);
        return this;
    }
}
