namespace Sencilla.Component.I18n;

public class GoogleTranslatorOptions
{
    public const string GoogleTranslator = "GoogleTranslator";
    public const string PrivateKeyParam = "{private_key}";

    public string ApiKey { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
    public string CredentialsFile { get; set; } = default!;
    public string Project { get; set; } = default!;
}
