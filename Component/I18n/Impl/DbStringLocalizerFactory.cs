namespace Sencilla.Component.I18n;

public class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILocalizationProvider _localizationProvider;

    public DbStringLocalizerFactory(ILocalizationProvider localizationProvider)
    {
        _localizationProvider = localizationProvider;
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new DbStringLocalizer(_localizationProvider);
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new DbStringLocalizer(_localizationProvider);
    }
}
