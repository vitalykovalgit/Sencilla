namespace Sencilla.Web;

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class FilterTypeBinderProvider : IModelBinderProvider
{
    private IList<IInputFormatter> _formatters;

    public FilterTypeBinderProvider(IList<IInputFormatter> formatters)
    {
        _formatters = formatters;
    }

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (typeof(IFilter).IsAssignableFrom(context.Metadata.ModelType))
        {
            //var attr = context.Metadata.ModelType.GetCustomAttribute<FromBodyAttribute>();

            // Get properties from entity type                 
            var type = GetFilterEntityType(context.Metadata.ModelType);
            var entityProperties = type == null ? new ModelMetadata[] { } : context.MetadataProvider.GetMetadataForProperties(type);

            // make array from them
            var arrayEntityProperties = new Dictionary<string, ModelMetadata>();
            foreach (var property in entityProperties)
            {
                if (!property.ModelType.IsArray)
                {
                    var metadata = context.MetadataProvider.GetMetadataForType(CollectionOf(property.ModelType));
                    arrayEntityProperties[property.ModelType.FullName] = metadata;
                }
            }

            // pass it to the binder 
            if (context.Metadata.IsComplexType)
            {
                var filterPropertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in context.Metadata.Properties)
                    filterPropertyBinders.Add(property, context.CreateBinder(property));

                var entityPropertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in entityProperties ?? Enumerable.Empty<ModelMetadata>())
                {
                    var binderMetadata = context.MetadataProvider.GetMetadataForType(CollectionOf(property.ModelType));
                    entityPropertyBinders.Add(property, context.CreateBinder(binderMetadata));
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var readerFactory = context.Services.GetRequiredService<IHttpRequestStreamReaderFactory>();

                if (context.BindingInfo.BindingSource == BindingSource.Body)
                {
                    return new FilterTypeBodyBinder(loggerFactory,
                        filterPropertyBinders,
                        entityPropertyBinders,
                        _formatters,
                        readerFactory,
                        context.MetadataProvider.GetMetadataForType(typeof(object)));
                }
                else
                {
                    return new FilterTypeBinder(loggerFactory, filterPropertyBinders, entityPropertyBinders, arrayEntityProperties);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The multi-value shape a query-string filter binds into: <c>List&lt;T&gt;</c>, never <c>T[]</c>.
    ///
    /// <para>For every type but one the two are interchangeable. The exception is <c>byte</c>: MVC binds
    /// <c>byte[]</c> with <see cref="ByteArrayModelBinder"/>, which reads a single BASE64 string. So
    /// <c>?publishStatus=3</c> against a <c>tinyint</c> column failed to parse, the binder returned no
    /// model, and <see cref="FilterTypeBinder"/> then dropped the criterion WITHOUT a word — a
    /// status-filtered admin page silently listed every row. <c>List&lt;byte&gt;</c> goes to the
    /// collection binder like every other type.</para>
    /// </summary>
    private static Type CollectionOf(Type propertyType)
        => typeof(List<>).MakeGenericType(propertyType);

    protected Type GetFilterEntityType(Type filterType)
    {
        Type baseType = filterType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var generic = baseType.GetGenericTypeDefinition();
                if (generic == typeof(Filter<>))
                    return baseType.GetGenericArguments()[0];
            }

            baseType = baseType.BaseType;
        }

        return null;
    }
}
