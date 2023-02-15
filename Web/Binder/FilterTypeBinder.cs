using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace Sencilla.Web
{
    [DisableInjection]
    public class FilterTypeBinder : IModelBinder
    {
        ComplexTypeModelBinder ComplexTypeModelBinder;

        IDictionary<ModelMetadata, IModelBinder> FilterProperties;
        IDictionary<ModelMetadata, IModelBinder> EntityProperties;
        IDictionary<string, ModelMetadata> EntityArrayProperties;

        public FilterTypeBinder(
            ILoggerFactory logger,
            IDictionary<ModelMetadata, IModelBinder> filterProperties,
            IDictionary<ModelMetadata, IModelBinder> entityProperties,
            IDictionary<string, ModelMetadata> entityArrayProperties)
        {
            FilterProperties = filterProperties;
            EntityProperties = entityProperties;
            EntityArrayProperties = entityArrayProperties;
            ComplexTypeModelBinder = new ComplexTypeModelBinder(filterProperties, logger);
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            // Bind values passed by user to filter
            await ComplexTypeModelBinder.BindModelAsync(bindingContext);

            var parameters = bindingContext.HttpContext.Request.Query;
            foreach (var propName in parameters.Keys)
            {
                // If they defined in filter skip it, so properties dictionary will contains only 
                // properties not defined in filter 
                if (FilterProperties.Any(p => p.Key.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Check if properties defined in entity 
                // Use only properties that are defined in entity 
                var property = EntityProperties.FirstOrDefault(p => p.Key.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                if (!property.Equals(default(KeyValuePair<ModelMetadata, IModelBinder>)))
                {
                    // Get values as array for the property 
                    ModelBindingResult result;
                    var propertyType = property.Key.ModelType;//Nullable.GetUnderlyingType(property.Key.ModelType) ?? property.Key.ModelType;

                    var metaArray = EntityArrayProperties[propertyType.FullName];
                    using (bindingContext.EnterNestedScope(metaArray, bindingContext.FieldName, propName, null))
                    {
                        await property.Value.BindModelAsync(bindingContext);
                        result = bindingContext.Result;
                    }

                    // Check if model is a filter and add to list of properties 
                    var filter = bindingContext.Model as IFilter;
                    if (filter != null)
                    {
                        foreach (var v in (System.Collections.IEnumerable)result.Model)
                            filter.AddProperty(propName, propertyType, v);
                    }
                }
                else
                {
                    // think how secure it is
                    var filter = bindingContext.Model as IFilter;
                    filter?.AddProperty(propName, null);
                }
            }
        }
    }
}
