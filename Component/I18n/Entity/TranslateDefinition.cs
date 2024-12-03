namespace Sencilla.Component.I18n;

public class TranslateDefinition
{
    public Translation Translation { get; set; }

    public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();

    public string Text { get; set; }
}
