namespace Sencilla.Mobile.Core.Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiUrlAttribute : System.Attribute
    {
        public ApiUrlAttribute(string url)
        {
            Url = url;
        }

        public string Url { get; private set; }
    }
}
