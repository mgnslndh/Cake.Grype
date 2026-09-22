using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace Cake.Grype.Json
{
    /// <summary>
    /// Builds the <see cref="IJsonTypeInfoResolver"/> used by <see cref="GrypeReportReader"/>.
    /// </summary>
    /// <remarks>
    /// A custom <see cref="System.Text.Json.Serialization.JsonConverter"/> cannot suspend mid-value, so a converter
    /// registered for every <see cref="IReadOnlyList{T}"/> would force System.Text.Json to buffer the whole raw array
    /// (for example <c>matches</c> or <c>ignoredMatches</c>) before it could run, defeating streaming for large
    /// reports. Instead, this resolver wraps the setter of every <see cref="IReadOnlyList{T}"/> property so an
    /// explicit JSON <c>null</c> becomes an empty array of the element type; the built-in, streaming collection
    /// converter still does the actual reading. A missing array is handled separately, by the
    /// <c>= Array.Empty&lt;T&gt;()</c> initializer already on each such property.
    /// </remarks>
    internal static class GrypeJsonTypeInfoResolver
    {
        /// <summary>
        /// Creates the resolver.
        /// </summary>
        /// <returns>The resolver.</returns>
        public static IJsonTypeInfoResolver Create()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(NormalizeNullReadOnlyLists);
            return resolver;
        }

        private static void NormalizeNullReadOnlyLists(JsonTypeInfo typeInfo)
        {
            foreach (var property in typeInfo.Properties)
            {
                if (!TryGetReadOnlyListElementType(property.PropertyType, out var elementType))
                {
                    continue;
                }

                var originalSet = property.Set;
                if (originalSet == null)
                {
                    continue;
                }

                var empty = Array.CreateInstance(elementType, 0);
                property.Set = (instance, value) => originalSet(instance, value ?? empty);
            }
        }

        private static bool TryGetReadOnlyListElementType(Type type, out Type elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }
    }
}
