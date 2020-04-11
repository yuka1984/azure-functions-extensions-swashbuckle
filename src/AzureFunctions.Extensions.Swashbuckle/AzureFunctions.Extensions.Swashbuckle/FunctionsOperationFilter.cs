using System.Collections.Generic;
using System.Linq;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    internal class FunctionsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            foreach (var customAttribute in context.MethodInfo.GetCustomAttributes(typeof(RequestHttpHeaderAttribute), false))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = (customAttribute as RequestHttpHeaderAttribute).HeaderName,
                    In = ParameterLocation.Header,
                    Schema = context.SchemaRepository.Schemas["string"],
                    Required = (customAttribute as RequestHttpHeaderAttribute).IsRequired
                });
            }

            foreach(var parameter in context.MethodInfo.GetParameters())
            {
                foreach (var customAttribute in parameter.GetCustomAttributes(typeof(RequestBodyTypeAttribute), false).OfType<RequestBodyTypeAttribute>())
                {
                    var type = customAttribute.Type;
                    var schema = context.SchemaRepository.GetOrAdd(type, type.Name, () => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository, parameterInfo: parameter));
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = customAttribute.Name ?? parameter.Name,
                        Description = customAttribute.Description,
                        Schema = schema,
                        Required = customAttribute.Required
                    });
                }
            }
            
        }
    }
}
