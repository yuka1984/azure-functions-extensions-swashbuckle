using System.Collections.Generic;

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

        public IDictionary<string, OptionSecurityScheme> SecurityDefinitions { get; set; } = new Dictionary<string, OptionSecurityScheme>();
    }

    public class OptionDocument
    {
        public string Name { get; set; } = "v1";
        public string Title { get; set; } = "Swashbuckle";

        public string Version { get; set; } = "v1";

        public string Description { get; set; } = "Swagger document by Swashbuckle";
    }

    public class OptionSecurityScheme
    {
        public string Type { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public string In { get; set; }

        public string Flow { get; set; }

        public string AuthorizationUrl { get; set; }

        public string TokenUrl { get; set; }

        public IDictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();
    }
}
