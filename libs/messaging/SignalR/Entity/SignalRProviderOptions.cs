namespace Sencilla.Messaging.SignalR;

public class SignalRProviderOptions
{
    public int MaxRetries { get; set; } = 5;
    public string HubPath { get; set; } = "/messaging";
    public string ConnectionString { get; set; } = string.Empty;
    public int ReconnectionDelay { get; set; } = 5000; // ms
}