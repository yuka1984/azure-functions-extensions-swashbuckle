using System;
using System.Net.Http;

namespace AzureFunctions.Extensions.Swashbuckle.Attribute
{
    /// <summary>
    /// Explicite body type definition for functions with <see cref="HttpRequestMessage"/> as input parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestBodyTypeAttribute : System.Attribute
    {
        /// <summary>
        /// A friendly name for the parameter
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Body model type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Body model description
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Body Required
        /// </summary>
        public bool Required { get; }

        /// <summary>
        /// Explicit body type definition for functions with <see cref="HttpRequestMessage"/> as input parameter
        /// </summary>
        /// <param name="bodyType">Body model type</param>
        /// <param name="description">Model description</param>
        /// <param name="name">An optional friendly name</param>
        /// <param name="required">Is the request body required</param>
        public RequestBodyTypeAttribute(Type bodyType, string description, string name = null, bool required = false)
        {
            Type = bodyType;
            Description = description;
            Name = name;
            Required = required;
        }
    }
}
