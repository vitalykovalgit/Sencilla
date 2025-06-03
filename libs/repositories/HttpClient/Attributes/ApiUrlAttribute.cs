namespace Sencilla.Core.Repo
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
