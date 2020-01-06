using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AzureFunctions.Extensions.Swashbuckle
{
    public static class TypeExtensions
    {

        public static bool IsEnumerable(this Type type, out Type itemType)
        {
            if (type.IsConstructedFrom(typeof(IEnumerable<>), out Type constructedType))
            {
                itemType = constructedType.GenericTypeArguments[0];
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                itemType = typeof(object);
                return true;
            }

            itemType = null;
            return false;
        }

        private static bool IsConstructedFrom(this Type type, Type genericType, out Type constructedType)
        {
            constructedType = new[] { type }
                .Union(type.GetInterfaces())
                .FirstOrDefault(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == genericType);

            return (constructedType != null);
        }

    }
}
