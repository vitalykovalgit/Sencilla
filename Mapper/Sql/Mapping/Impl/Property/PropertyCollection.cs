using System.Collections.Generic;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Property
{
    public class PropertyCollection<TEntity> : Dictionary<string, IPropertyMapping<TEntity>>, IPropertyCollection, IPropertyCollection<TEntity>
    {
        IEnumerator<IPropertyMapping<TEntity>> IEnumerable<IPropertyMapping<TEntity>>.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public new IEnumerator<IPropertyMapping> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public void Add(IPropertyMapping<TEntity> property)
        {
            if (!string.IsNullOrEmpty(property?.Name))
                this[property.Name.ToLower()] = property;
        }

        public IPropertyMapping<TEntity> GetProperty(string name)
        {
            return base[name.ToLower()];
        }
    }
}
