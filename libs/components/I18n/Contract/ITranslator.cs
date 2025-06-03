namespace Sencilla.Component.I18n;

public interface ITranslator
{
    /// <summary>
    /// 
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 
    /// </summary>
    bool Default { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<string[]> GetSupportedLanguages();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    Task<string[]> TranslateText(string[] values, string from, string to);
    
}
