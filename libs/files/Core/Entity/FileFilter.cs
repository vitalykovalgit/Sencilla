namespace Sencilla.Component.Files;

[DisableInjection]
public class FileFilter : Filter
{
    public FileFilter ByParentId(params Guid[] filesId)
    {
        foreach (var fileId in filesId)
            AddProperty(nameof(File.ParentId), typeof(Guid), fileId);

        return this;
    }

    public FileFilter ByDimmension(int? dim)
    {
        AddProperty(nameof(File.Dim), typeof(int), dim);
        return this;
    }
}
