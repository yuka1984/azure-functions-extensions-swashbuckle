using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    public static class SwashBuckleStartupExtension
    {
        public static IWebJobsBuilder AddSwashBuckle(this IWebJobsBuilder builder, Assembly assembly, Action<Option> configure = null)
        {
            builder.AddExtension<SwashbuckleConfig>()
                .BindOptions<Option>()
                .ConfigureOptions<Option>((config, section, options) => { configure?.Invoke(options); })
                .Services.AddSingleton(new SwashBuckleStartupConfig
                {
                    Assembly = assembly
                });

            builder.Services.AddSingleton<IOutputFormatter>(c =>
                new JsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Create()));

            builder.Services.AddSingleton<IModelMetadataProvider>(c => new DefaultModelMetadataProvider(
                new DefaultCompositeMetadataDetailsProvider(
                    new List<IMetadataDetailsProvider>()
                    {
                        new DefaultValidationMetadataProvider(),
                    })));

            builder.Services.AddSingleton<IApiDescriptionGroupCollectionProvider, FunctionApiDescriptionProvider>();

            return builder;
        }

        /// <summary>
        /// A default implementation of <see cref="ICompositeMetadataDetailsProvider"/>.
        /// </summary>
        internal class DefaultCompositeMetadataDetailsProvider : ICompositeMetadataDetailsProvider
        {
            private readonly IEnumerable<IMetadataDetailsProvider> _providers;

            /// <summary>
            /// Creates a new <see cref="DefaultCompositeMetadataDetailsProvider"/>.
            /// </summary>
            /// <param name="providers">The set of <see cref="IMetadataDetailsProvider"/> instances.</param>
            public DefaultCompositeMetadataDetailsProvider(IEnumerable<IMetadataDetailsProvider> providers)
            {
                _providers = providers;
            }

            /// <inheritdoc />
            public void CreateBindingMetadata(BindingMetadataProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                foreach (var provider in _providers.OfType<IBindingMetadataProvider>())
                {
                    provider.CreateBindingMetadata(context);
                }
            }

            /// <inheritdoc />
            public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                foreach (var provider in _providers.OfType<IDisplayMetadataProvider>())
                {
                    provider.CreateDisplayMetadata(context);
                }
            }

            /// <inheritdoc />
            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                foreach (var provider in _providers.OfType<IValidationMetadataProvider>())
                {
                    provider.CreateValidationMetadata(context);
                }
            }
        }

        /// <summary>
        /// A default implementation of <see cref="IValidationMetadataProvider"/>.
        /// </summary>
        internal class DefaultValidationMetadataProvider : IValidationMetadataProvider
        {
            /// <inheritdoc />
            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                foreach (var attribute in context.Attributes)
                {
                    if (attribute is IModelValidator || attribute is IClientModelValidator)
                    {
                        // If another provider has already added this attribute, do not repeat it.
                        // This will prevent attributes like RemoteAttribute (which implement ValidationAttribute and
                        // IClientModelValidator) to be added to the ValidationMetadata twice.
                        // This is to ensure we do not end up with duplication validation rules on the client side.
                        if (!context.ValidationMetadata.ValidatorMetadata.Contains(attribute))
                        {
                            context.ValidationMetadata.ValidatorMetadata.Add(attribute);
                        }
                    }
                }

                // IPropertyValidationFilter attributes on a type affect properties in that type, not properties that have
                // that type. Thus, we ignore context.TypeAttributes for properties and not check at all for types.
                if (context.Key.MetadataKind == ModelMetadataKind.Property)
                {
                    var validationFilter = context.PropertyAttributes.OfType<IPropertyValidationFilter>().FirstOrDefault();
                    if (validationFilter == null)
                    {
                        // No IPropertyValidationFilter attributes on the property.
                        // Check if container has such an attribute.
                        validationFilter = context.Key.ContainerType.GetTypeInfo()
                            .GetCustomAttributes(inherit: true)
                            .OfType<IPropertyValidationFilter>()
                            .FirstOrDefault();
                    }

                    context.ValidationMetadata.PropertyValidationFilter = validationFilter;
                }
            }
        }

        internal static class MediaTypeHeaderValues
        {
            public static readonly MediaTypeHeaderValue ApplicationJson
                = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

            public static readonly MediaTypeHeaderValue TextJson
                = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

            public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax
                = MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();

            public static readonly MediaTypeHeaderValue ApplicationXml
                = MediaTypeHeaderValue.Parse("application/xml").CopyAsReadOnly();

            public static readonly MediaTypeHeaderValue TextXml
                = MediaTypeHeaderValue.Parse("text/xml").CopyAsReadOnly();

            public static readonly MediaTypeHeaderValue ApplicationAnyXmlSyntax
                = MediaTypeHeaderValue.Parse("application/*+xml").CopyAsReadOnly();
        }

        internal class JsonArrayPool<T> : IArrayPool<T>
        {
            private readonly ArrayPool<T> _inner;

            public JsonArrayPool(ArrayPool<T> inner)
            {
                if (inner == null)
                {
                    throw new ArgumentNullException(nameof(inner));
                }

                _inner = inner;
            }

            public T[] Rent(int minimumLength)
            {
                return _inner.Rent(minimumLength);
            }

            public void Return(T[] array)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                _inner.Return(array);
            }
        }

        /// <summary>
        /// A <see cref="TextOutputFormatter"/> for JSON content.
        /// </summary>
        internal class JsonOutputFormatter : TextOutputFormatter
        {
            private readonly IArrayPool<char> _charPool;

            // Perf: JsonSerializers are relatively expensive to create, and are thread safe. We cache
            // the serializer and invalidate it when the settings change.
            private JsonSerializer _serializer;

            /// <summary>
            /// Initializes a new <see cref="JsonOutputFormatter"/> instance.
            /// </summary>
            /// <param name="serializerSettings">
            /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
            /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
            /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
            /// </param>
            /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
            public JsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool)
            {
                if (serializerSettings == null)
                {
                    throw new ArgumentNullException(nameof(serializerSettings));
                }

                if (charPool == null)
                {
                    throw new ArgumentNullException(nameof(charPool));
                }

                SerializerSettings = serializerSettings;
                _charPool = new JsonArrayPool<char>(charPool);

                SupportedEncodings.Add(Encoding.UTF8);
                SupportedEncodings.Add(Encoding.Unicode);
                SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
                SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
                SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
            }

            /// <summary>
            /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
            /// </summary>
            /// <remarks>
            /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
            /// <see cref="JsonOutputFormatter"/> has been used will have no effect.
            /// </remarks>
            protected JsonSerializerSettings SerializerSettings { get; }

            /// <summary>
            /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
            /// </summary>
            /// <remarks>
            /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
            /// <see cref="JsonOutputFormatter"/> has been used will have no effect.
            /// </remarks>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public JsonSerializerSettings PublicSerializerSettings => SerializerSettings;

            /// <summary>
            /// Writes the given <paramref name="value"/> as JSON using the given
            /// <paramref name="writer"/>.
            /// </summary>
            /// <param name="writer">The <see cref="TextWriter"/> used to write the <paramref name="value"/></param>
            /// <param name="value">The value to write as JSON.</param>
            public void WriteObject(TextWriter writer, object value)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                using (var jsonWriter = CreateJsonWriter(writer))
                {
                    var jsonSerializer = CreateJsonSerializer();
                    jsonSerializer.Serialize(jsonWriter, value);
                }
            }

            /// <summary>
            /// Called during serialization to create the <see cref="JsonWriter"/>.
            /// </summary>
            /// <param name="writer">The <see cref="TextWriter"/> used to write.</param>
            /// <returns>The <see cref="JsonWriter"/> used during serialization.</returns>
            protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                var jsonWriter = new JsonTextWriter(writer)
                {
                    ArrayPool = _charPool,
                    CloseOutput = false,
                    AutoCompleteOnClose = false
                };

                return jsonWriter;
            }

            /// <summary>
            /// Called during serialization to create the <see cref="JsonSerializer"/>.
            /// </summary>
            /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
            protected virtual JsonSerializer CreateJsonSerializer()
            {
                if (_serializer == null)
                {
                    _serializer = JsonSerializer.Create(SerializerSettings);
                }

                return _serializer;
            }

            /// <inheritdoc />
            public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (selectedEncoding == null)
                {
                    throw new ArgumentNullException(nameof(selectedEncoding));
                }

                var response = context.HttpContext.Response;
                using (var writer = context.WriterFactory(response.Body, selectedEncoding))
                {
                    WriteObject(writer, context.Object);

                    // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                    // buffers. This is better than just letting dispose handle it (which would result in a synchronous
                    // write).
                    await writer.FlushAsync();
                }
            }
        }
    }
}