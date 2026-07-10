using System.Globalization;
using System.Text.Json.Serialization;
using Mono.Cecil;

namespace CSharp2TS.CLI.Utility {
    public record JsonDerivedTypeInfo(TypeDefinition Type, object? Discriminator);

    /// <summary>
    /// Reads the System.Text.Json polymorphism attributes ([JsonPolymorphic] / [JsonDerivedType])
    /// so polymorphic types can be generated as TypeScript discriminated unions.
    /// </summary>
    public static class JsonPolymorphismUtility {
        // System.Text.Json's default discriminator property name
        public const string DefaultDiscriminatorPropertyName = "$type";

        public static bool IsPolymorphicRoot(TypeDefinition typeDef) {
            return typeDef.HasAttribute<JsonDerivedTypeAttribute>();
        }

        public static IList<JsonDerivedTypeInfo> GetDerivedTypes(TypeDefinition typeDef) {
            return typeDef.CustomAttributes
                .Where(a => a.AttributeType.FullName == typeof(JsonDerivedTypeAttribute).FullName)
                .Select(a => new JsonDerivedTypeInfo(
                    ((TypeReference)a.ConstructorArguments[0].Value).Resolve(),
                    a.ConstructorArguments.Count > 1 ? a.ConstructorArguments[1].Value : null))
                .ToList();
        }

        public static string GetDiscriminatorPropertyName(TypeDefinition typeDef) {
            if (typeDef.TryGetAttribute<JsonPolymorphicAttribute>(out var attribute) &&
                attribute.TryGetAttributeValue<string>(nameof(JsonPolymorphicAttribute.TypeDiscriminatorPropertyName), out var name)) {
                return name!;
            }

            return DefaultDiscriminatorPropertyName;
        }

        /// <summary>
        /// Finds the discriminator a type is registered with on its polymorphic base type,
        /// e.g. [JsonDerivedType(typeof(ThisType), "discriminator")].
        /// </summary>
        public static bool TryGetDiscriminator(TypeDefinition typeDef, out string propertyName, out string tsLiteral) {
            var baseType = typeDef.BaseType;

            while (baseType != null && baseType.FullName != "System.Object") {
                var baseTypeDef = baseType.Resolve();
                var derivedType = GetDerivedTypes(baseTypeDef).FirstOrDefault(i => i.Type.FullName == typeDef.FullName);

                if (derivedType != null && derivedType.Discriminator != null) {
                    propertyName = GetDiscriminatorPropertyName(baseTypeDef);
                    tsLiteral = FormatDiscriminatorLiteral(derivedType.Discriminator);

                    return true;
                }

                baseType = baseTypeDef.BaseType;
            }

            propertyName = string.Empty;
            tsLiteral = string.Empty;

            return false;
        }

        public static string FormatDiscriminatorLiteral(object discriminator) {
            if (discriminator is string value) {
                return $"'{value.Replace("'", "\\'")}'";
            }

            return Convert.ToString(discriminator, CultureInfo.InvariantCulture)!;
        }
    }
}
