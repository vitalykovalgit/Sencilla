
namespace Sencilla.Core.Repo
{
    /// <summary>
    /// If this atribute placed on property 
    /// this property will not be serialized 
    /// into query 
    /// </summary>
    public class SkipInUrlParamsAttribute : System.Attribute
    {
        public SkipInUrlParamsAttribute()
        {
        }
    }
}
