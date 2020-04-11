using System;
using System.Collections.Generic;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    public class Option
    {

        public string Title { get; set; } = "AzureFunctions.Extensions.Swashbuckle";
        public string XmlPath { get; set; }
        public bool AddCodeParamater { get; set; } = true;

        public OptionDocument[] Documents { get; set; } = { };

        public bool PrepandOperationWithRoutePrefix { get; set; } = true;

        public bool FillSwaggerBasePathWithRoutePrefix { get; set; } = false;

        public Action<SwaggerGenOptions> SwaggerConfigurator { get; set; } = (x) => {};
        public OpenApiSpecVersion OpenApiSpec { get; set; } = OpenApiSpecVersion.OpenApi3_0;
    }

    public class OptionDocument
    {
        public string Name { get; set; } = "v1";
        public string Title { get; set; } = "Swashbuckle";

        public string Version { get; set; } = "v1";

        public string Description { get; set; } = "Swagger document by Swashbuckle";
    }
}
