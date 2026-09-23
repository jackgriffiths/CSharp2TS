using CSharp2TS.CLI.Generators.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Mono.Cecil;
using System.Text.Json;

namespace CSharp2TS.CLI.Generators.Common {
    public static class TSTypeMapper {
        private static readonly Type[] stringTypes = [
            typeof(char), typeof(string), typeof(Guid),
            typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly),
            typeof(TimeOnly), typeof(byte[])
        ];
        private static readonly Type[] voidTypes = [typeof(void), typeof(Task), typeof(ActionResult), typeof(IActionResult)];
        private static readonly Type[] fileCollectionTypes = [typeof(FormFileCollection), typeof(IFormFileCollection)];
        private static readonly Type[] fileReturnTypes = [typeof(FileContentResult), typeof(FileStreamResult), typeof(FileResult)];
        private static readonly Type[] fileTypes = [typeof(FormFile), typeof(IFormFile), .. fileCollectionTypes, .. fileReturnTypes];
        private static readonly Type[] formDataTypes = [typeof(IFormCollection)];
        private static readonly Type[] unknownTypes = [typeof(JsonElement)];
        private static readonly Type[] numberTypes = [
            typeof(sbyte), typeof(byte), typeof(short),
            typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float),
            typeof(double), typeof(decimal)
        ];

        public static TSType GetTSPropertyType(TypeReference type, Options options, Func<string, string, bool>? importHandler = null) {
            string tsType;
            List<TSType> genericArguments = [];

            TryExtractFromGenericIfRequired(typeof(Task<>), ref type);
            TryExtractFromGenericIfRequired(typeof(ActionResult<>), ref type);

            int jaggedCount = 0;
            bool isEnum = false;
            bool isDictionary = TryExtractFromDictionary(ref type);
            bool isCollection = TryExtractFromCollection(ref type, ref jaggedCount);
            bool isNullable = TryExtractFromGenericIfRequired(typeof(Nullable<>), ref type);

            if (type.IsGenericInstance) {
                var generic = (GenericInstanceType)type;

                foreach (var arg in generic.GenericArguments) {
                    genericArguments.Add(GetTSPropertyType(arg, options, importHandler));
                }
            }

            if (options.CustomTypeMappings.TryGetValue(type.FullName, out string? customTsType)) {
                tsType = customTsType;
            } else if (stringTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.String;

                if (!isNullable && SimpleTypeCheck(type, typeof(string)) && options.UseNullableStrings) {
                    isNullable = true;
                }
            } else if (numberTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.Number;
            } else if (type.FullName == typeof(bool).FullName) {
                tsType = TSTypeConsts.Boolean;
            } else if (voidTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.Void;
            } else if (fileTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.File;

                if (fileCollectionTypes.Any(i => SimpleTypeCheck(type, i))) {
                    isCollection = true;
                    jaggedCount = 1;
                }
            } else if (formDataTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.FormData;
            } else if (unknownTypes.Any(i => SimpleTypeCheck(type, i))) {
                tsType = TSTypeConsts.Unknown;
            } else {
                tsType = GetCleanedTypeName(type) ?? TSTypeConsts.Object;
                importHandler?.Invoke(type.Resolve()?.FullName ?? type.FullName, tsType);

                isEnum = type.Resolve()?.IsEnum ?? false;
            }

            return new TSType {
                TypeName = tsType,
                GenericArguments = genericArguments,
                IsCollection = isCollection,
                JaggedCount = jaggedCount,
                IsDictionary = isDictionary,
                IsNullable = isNullable,
                IsEnum = isEnum,
            };
        }

        private static bool SimpleTypeCheck(TypeReference typeReference, Type type) {
            return typeReference.FullName == type.FullName;
        }

        private static bool TryExtractFromDictionary(ref TypeReference type) {
            if (!type.IsGenericInstance) {
                return false;
            }

            bool isDictionary = HasInterface(type, typeof(IDictionary<,>));

            // If it's a dictionary type, extract the generic arguments
            if (isDictionary && type is GenericInstanceType genericInstance && genericInstance.GenericArguments.Count > 1) {
                type = genericInstance.GenericArguments[1];
                return true;
            }

            return false;
        }

        private static bool TryExtractFromCollection(ref TypeReference type, ref int currentIteration) {
            if (type.IsArray) {
                // byte[] is not a collection - JSON serializers write it as a base64 string
                if (SimpleTypeCheck(type, typeof(byte[]))) {
                    return false;
                }

                type = ((ArrayType)type).ElementType;
                currentIteration++;

                TryExtractFromCollection(ref type, ref currentIteration);

                return true;
            }

            if (!type.IsGenericInstance) {
                return false;
            }

            bool isCollection = HasInterface(type, typeof(IEnumerable<>));

            // If it's a collection type, extract the generic argument
            if (isCollection && type is GenericInstanceType genericInstance && genericInstance.GenericArguments.Count > 0) {
                type = genericInstance.GenericArguments[0];
                currentIteration++;

                TryExtractFromCollection(ref type, ref currentIteration);

                return true;
            }

            return false;
        }

        private static bool HasInterface(TypeReference type, Type implementsType) {
            return type.GetElementType().FullName == implementsType.FullName ||
                type.Resolve().Interfaces
                    .Where(i => i.InterfaceType.IsGenericInstance)
                    .Where(i => SimpleTypeCheck(i.InterfaceType.GetElementType(), implementsType))
                    .Any();
        }

        private static bool TryExtractFromGenericIfRequired(Type type, ref TypeReference typeRef) {
            if (typeRef.IsGenericParameter) {
                return false;
            }

            if (typeRef.Resolve().FullName != type.FullName) {
                return false;
            }

            typeRef = ((GenericInstanceType)typeRef).GenericArguments[0];

            return true;
        }

        private static string GetCleanedTypeName(TypeReference type) {
            if (type.HasGenericParameters || type is GenericInstanceType genericType && genericType.GenericArguments.Count > 0) {
                return type.Name.Split('`')[0];
            }

            return type.Name;
        }
    }
}
