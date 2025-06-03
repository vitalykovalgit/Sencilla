namespace Sencilla.Component.I18n;

public interface ILocalizationProvider
{
    Task<string?> GetString(string resourceKey, string locale);
    Task<Dictionary<string, string>> GetStrings(string locale);
    Task<Dictionary<string, string>> GetStringsByGroup(string ns, string locale);
}
