namespace Sencilla.Component.I18n;

public class GoogleTranslatorOptions
{
    public const string GoogleTranslator = "GoogleTranslator";
    public const string PrivateKeyParam = "{private_key}";

    public string ApiKey { get; set; }
    public string PrivateKey { get; set; }
    public string CredentialsFile { get; set; }
    public string Project { get; set; }
}
