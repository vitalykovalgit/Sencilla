
using System.Collections.Generic;

namespace Sencilla.Component.Localization
{
    public interface ILocalization
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="stringId"></param>
        /// <returns></returns>
        Translate GetTranslate(uint lang, string stringId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        IEnumerable<Translate> GetTranslates(uint lang);
    }
}
