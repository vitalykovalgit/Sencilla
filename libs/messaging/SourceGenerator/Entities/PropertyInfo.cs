namespace Sencilla.Messaging.SourceGenerator;

internal class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);
}
