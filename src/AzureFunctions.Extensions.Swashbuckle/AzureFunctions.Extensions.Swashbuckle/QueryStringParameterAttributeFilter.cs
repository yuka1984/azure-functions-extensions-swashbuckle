using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Extensions.Swashbuckle
{
    internal class QueryStringParameterAttributeFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var attributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<QueryStringParameterAttribute>();

            foreach (var attribute in attributes)
            {
                Schema type = context.SchemaRegistry.GetOrRegister(attribute.DataType ?? typeof(string));
                var parameter = new NonBodyParameter
                {
                    Name = attribute.Name,
                    Description = attribute.Description,
                    In = "query",
                    Required = attribute.Required,
                    Type = type.Type,
                    Format = type.Format
                };

                if (attribute.DataType != null && attribute.DataType.IsEnumerable(out Type _) && attribute.DataType != typeof(string)) // string is an enumerable, but should not be handled as an array
                {
                    parameter.CollectionFormat = "multi";
                    parameter.Items = new PartialSchema { Type = type.Items.Type, Format = type.Items.Format};
                }

                operation.Parameters.Add(parameter);
            }
        }
    }
}
