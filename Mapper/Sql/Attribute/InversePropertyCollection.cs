namespace System.ComponentModel.DataAnnotations.Schema
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class InversePropertyCollectionAttribute : System.Attribute
    {
        public InversePropertyCollectionAttribute(string property)
        {
            Property = property;
        }

        public string Property { get; }
    }
}
