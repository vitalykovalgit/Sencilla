namespace Sencilla.Mobile.Core.Attribute
{
    /// <summary>
    /// If this atribute placed on property 
    /// this property will not be serialized 
    /// into query 
    /// </summary>
    public class ApiSkipInUrlAttribute : System.Attribute
    {
        public ApiSkipInUrlAttribute()
        {
        }
    }
}
