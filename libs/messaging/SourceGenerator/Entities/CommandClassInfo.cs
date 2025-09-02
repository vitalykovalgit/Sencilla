namespace Sencilla.Messaging.SourceGenerator;

internal class CommandClassInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public List<PropertyInfo> Properties { get; set; } = new();
}
