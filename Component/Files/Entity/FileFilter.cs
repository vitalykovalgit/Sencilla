namespace Sencilla.Component.Files;

public class FileFilter : Filter
{
    public FileFilter ByOriginalId(params Guid[] filesId)
    {
        foreach (var fileId in filesId)
            AddProperty(nameof(File.OriginalFileId), typeof(Guid), fileId);

        return this;
    }

    public FileFilter ByDimmension(int? dim)
    {
        AddProperty(nameof(File.Dim), typeof(int), dim);
        return this;
    }
}
