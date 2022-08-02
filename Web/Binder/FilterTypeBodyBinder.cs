using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sencilla.Core;

namespace Deloitte.PursuitFramework.Impl
{
    public class FilterTypeBodyBinder : IModelBinder
    {
        BodyModelBinder BodyModelBinder;

        IDictionary<ModelMetadata, IModelBinder> FilterProperties;
        IDictionary<ModelMetadata, IModelBinder> EntityProperties;

        private readonly IList<IInputFormatter> _formatters;
        private readonly ModelMetadata _dictMetadata;
        private readonly IHttpRequestStreamReaderFactory _readerFactory;

        public FilterTypeBodyBinder(
            ILoggerFactory logger,
            IDictionary<ModelMetadata, IModelBinder> filterProperties,
            IDictionary<ModelMetadata, IModelBinder> entityProperties,
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ModelMetadata dictMetadata)
        {
            FilterProperties = filterProperties;
            EntityProperties = entityProperties;
            BodyModelBinder = new BodyModelBinder(formatters, readerFactory);

            _formatters = formatters;
            _readerFactory = readerFactory;
            _dictMetadata = dictMetadata;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            await BodyModelBinder.BindModelAsync(bindingContext);
            await BindDynamicPropertiesAsync(bindingContext);
        }

        public async Task BindDynamicPropertiesAsync(ModelBindingContext bindingContext)
        {
            var modelBindingKey = bindingContext.IsTopLevelObject
                ? bindingContext.BinderModelName ?? string.Empty
                : bindingContext.ModelName;

            var httpContext = bindingContext.HttpContext;

            var stream = httpContext.Request.Body;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            var formatterContext = new InputFormatterContext(
                httpContext,
                modelBindingKey,
                bindingContext.ModelState,
                _dictMetadata,
                _readerFactory.CreateReader);

            var formatter = (IInputFormatter)null;
            for (var i = 0; i < _formatters.Count; i++)
            {
                if (_formatters[i].CanRead(formatterContext))
                {
                    formatter = _formatters[i];
                    break;
                }
            }

            if (formatter == null)
                return;

            var result = await formatter.ReadAsync(formatterContext);
            if (result.HasError)
                return;

            if (result?.Model != null)
            {

                foreach (var kvp in ((JObject)result.Model).Children())
                {
                    var param = (JProperty)kvp;

                    if (FilterProperties.Any(p => p.Key.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var property = EntityProperties.FirstOrDefault(p =>
                        p.Key.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                    if (!property.Equals(default(KeyValuePair<ModelMetadata, IModelBinder>)))
                    {
                        var filter = bindingContext.Result.Model as IFilter;
                        if (filter != null)
                        {
                            if (param.Value is JValue)
                            {
                                if (CheckType(param.Value as JValue, property.Key.ModelType))
                                {
                                    filter.AddProperty(param.Name, ((JValue)param.Value).Value);
                                }
                            }
                            else if (param.Value is JArray)
                            {
                                var values = ((JArray)param.Value).Values()
                                    .Where(p => p is JValue)
                                    .Where(p => CheckType(p as JValue, property.Key.ModelType))
                                    .Select(p => ((JValue)p).Value)
                                    .ToArray();

                                if (values.Count() > 0)
                                    filter.AddProperty(param.Name, values);
                            }
                        }
                    }
                }
            }
        }

        private bool CheckType(JValue jValue, Type type)
        {
            if (jValue == null)
                throw new ArgumentNullException(nameof(jValue));

            var underType = Nullable.GetUnderlyingType(type);
            if (underType != null)
                type = underType;

            return jValue.Type.ToString() == type.Name;
        }
    }
}
