using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    internal class QueryStringParameterAttributeFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<QueryStringParameterAttribute>();

            foreach (var attribute in attributes)
            {
                var type = attribute.DataType ?? typeof(string);

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = attribute.Name,
                    Description = attribute.Description,
                    In = ParameterLocation.Query,
                    Required = attribute.Required,
                    Schema = context.SchemaRepository.GetOrAdd(type,type.Name, () => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository))
                });
            }
        }
    }
}
