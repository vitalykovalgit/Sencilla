namespace Sencilla.Component.I18n;

/// <summary>
/// TODO: Move to geo component and use from there
/// </summary>
public class Language : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string NativeName { get; set; } = default!;

    public string Locale { get; set; } = default!;

    public int Order { set; get; }
}
