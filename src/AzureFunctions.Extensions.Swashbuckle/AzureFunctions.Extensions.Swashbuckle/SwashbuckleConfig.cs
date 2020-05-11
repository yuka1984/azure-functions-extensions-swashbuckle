﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    [Extension("Swashbuckle", "Swashbuckle")]
    internal class SwashbuckleConfig : IExtensionConfigProvider, IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
        private readonly Option _option;
        private readonly string _xmlPath;
        private ServiceProvider _serviceProvider;
        private readonly HttpOptions _httpOptions;

        public string RoutePrefix => _httpOptions.RoutePrefix;

        private static readonly Lazy<string> IndexHtml = new Lazy<string>(() =>
        {
            var indexHtml = "";
            using (var stream = Assembly.GetAssembly(typeof(SwashbuckleConfig))
                .GetManifestResourceStream($"{typeof(SwashbuckleConfig).Namespace}.EmbededResources.index.html"))
            using (var reader = new StreamReader(stream))
            {
                indexHtml = reader.ReadToEnd();
            }

            using (var stream = Assembly.GetAssembly(typeof(SwashbuckleConfig))
                .GetManifestResourceStream($"{typeof(SwashbuckleConfig).Namespace}.EmbededResources.swagger-ui.css"))
            using (var reader = new StreamReader(stream))
            {
                var style = reader.ReadToEnd();
                indexHtml = indexHtml.Replace("{style}", style);
            }

            using (var stream = Assembly.GetAssembly(typeof(SwashbuckleConfig))
                .GetManifestResourceStream($"{typeof(SwashbuckleConfig).Namespace}.EmbededResources.swagger-ui-bundle.js"))
            using (var reader = new StreamReader(stream))
            {
                var bundlejs = reader.ReadToEnd();
                indexHtml = indexHtml.Replace("{bundle.js}", bundlejs);
            }

            using (var stream = Assembly.GetAssembly(typeof(SwashbuckleConfig))
                .GetManifestResourceStream($"{typeof(SwashbuckleConfig).Namespace}.EmbededResources.swagger-ui-standalone-preset.js"))
            using (var reader = new StreamReader(stream))
            {
                var presetjs = reader.ReadToEnd();
                indexHtml = indexHtml.Replace("{standalone-preset.js}", presetjs);
            }
            return indexHtml;
        });

        private readonly Lazy<string> _indexHtmLazy;


        public SwashbuckleConfig(
            IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
            IOptions<Option> functionsOptions,
            SwashBuckleStartupConfig startupConfig,
            IOptions<HttpOptions> httpOptions)
        {
            _apiDescriptionGroupCollectionProvider = apiDescriptionGroupCollectionProvider;
            _option = functionsOptions.Value;
            _httpOptions = httpOptions.Value;
            if (!string.IsNullOrWhiteSpace(_option.XmlPath))
            {
                var binPath = Path.GetDirectoryName(startupConfig.Assembly.Location);
                var binDirectory = Directory.CreateDirectory(binPath);
                var xmlBasePath = binDirectory?.Parent?.FullName;
                var xmlPath = Path.Combine(xmlBasePath, _option.XmlPath);
                if (File.Exists(xmlPath))
                {
                    _xmlPath = xmlPath;
                }
            }
            _indexHtmLazy = new Lazy<string>(() => IndexHtml.Value.Replace("{title}", _option.Title));
        }

        public string GetSwaggerUIContent(string swaggerUrl)
        {
            var html = _indexHtmLazy.Value;
            return html.Replace("{url}", swaggerUrl);
        }

        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<SwashBuckleClientAttribute>()
                .Bind(new SwashBuckleClientBindingProvider(this));

            var services = new ServiceCollection();

            services.AddSingleton<IApiDescriptionGroupCollectionProvider>(_apiDescriptionGroupCollectionProvider);
            services.AddSwaggerGen(options =>
            {
                if (_option.Documents.Length == 0)
                {
                    var defaultDocument = new OptionDocument();
                    AddSwaggerDocument(options, defaultDocument);
                }
                else
                {
                    foreach (var optionDocument in _option.Documents)
                    {
                        AddSwaggerDocument(options, optionDocument);
                    }
                }

                foreach (var optionSecurityDefinition in _option.SecurityDefinitions)
                {
                    AddSwaggerSecurityDefinition(options, optionSecurityDefinition.Key, optionSecurityDefinition.Value);
                }

                options.DescribeAllEnumsAsStrings();

                if (!string.IsNullOrWhiteSpace(_xmlPath))
                {
                    options.IncludeXmlComments(_xmlPath);
                }
                options.OperationFilter<FunctionsOperationFilter>();
                options.OperationFilter<QueryStringParameterAttributeFilter>();
                options.OperationFilter<GenerateOperationIdFilter>();
            });

            _serviceProvider = services.BuildServiceProvider(true);
        }

        public Stream GetSwaggerDocument(string host, string documentName = "v1")
        {
            var requiredService = _serviceProvider.GetRequiredService<ISwaggerProvider>();
            string basePath = _option.FillSwaggerBasePathWithRoutePrefix ? $"/{RoutePrefix}" : null;
            var swaggerDocument = requiredService.GetSwagger(documentName, host, basePath);
            var mem = new MemoryStream();
            var streamWriter = new StreamWriter(mem);
            var mvcOptionsAccessor =
                (IOptions<MvcJsonOptions>)_serviceProvider.GetService(typeof(IOptions<MvcJsonOptions>));
            var serializer = SwaggerSerializerFactory.Create(mvcOptionsAccessor);
            serializer.Serialize(streamWriter, swaggerDocument);
            streamWriter.Flush();
            mem.Position = 0;
            return mem;
        }

        public Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static void AddSwaggerDocument(SwaggerGenOptions options, OptionDocument document)
        {
            options.SwaggerDoc(document.Name, new Info
            {
                Title = document.Title,
                Version = document.Version,
                Description = document.Description,
            });
        }

        private static void AddSwaggerSecurityDefinition(SwaggerGenOptions options, string name, OptionSecurityScheme securityScheme)
        {
            switch (securityScheme?.Type)
            {
                case "apiKey":

                    options.AddSecurityDefinition(name,
                        new ApiKeyScheme
                        {
                            Description = securityScheme.Description,
                            Name = securityScheme.Name,
                            In = securityScheme.In
                        });

                    options.AddSecurityRequirement(
                        new Dictionary<string, IEnumerable<string>>
                        {
                            {name, new string[0]}
                        });

                    break;
                case "basic":

                    options.AddSecurityDefinition(name,
                        new BasicAuthScheme
                        {
                            Description = securityScheme.Description
                        });

                    options.AddSecurityRequirement(
                        new Dictionary<string, IEnumerable<string>>
                        {
                            {name, new string[0]}
                        });

                    break;
                case "oauth2":

                    options.AddSecurityDefinition(name,
                        new OAuth2Scheme
                        {
                            Description = securityScheme.Description,
                            Flow = securityScheme.Flow,
                            AuthorizationUrl = securityScheme.AuthorizationUrl,
                            TokenUrl = securityScheme.TokenUrl,
                            // host.json parser would not allow colons in json keys
                            Scopes = securityScheme.Scopes.ToDictionary(
                                s => s.Key.Replace("%3a", ":"),
                                s => s.Value.Replace("%3a", ":"))
                        });

                    options.AddSecurityRequirement(
                        new Dictionary<string, IEnumerable<string>>
                        {
                            {name, securityScheme.Scopes.Keys.Select(s => s.Replace("%3a", ":"))}
                        });

                    break;

                default:

                    break;
            }
        }
    }
}
