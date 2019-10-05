namespace Sencilla.Impl.Repository.HttpClient.Attributes
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
