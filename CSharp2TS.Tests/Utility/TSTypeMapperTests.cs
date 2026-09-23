using CSharp2TS.CLI;
using CSharp2TS.CLI.Generators.Common;
using CSharp2TS.CLI.Generators.Entities;
using Mono.Cecil;

namespace CSharp2TS.Tests.Utility {
    public class TSTypeMapperTests {
        private ModuleDefinition module = null!;
        private Options options = null!;

        [SetUp]
        public void Setup() {
            // Load the test assembly to get TypeReferences
            string assemblyPath = typeof(TSTypeMapperTests).Assembly.Location;
            module = ModuleDefinition.ReadModule(assemblyPath);

            options = new Options {
                UseNullableStrings = false
            };
        }

        [TearDown]
        public void TearDown() {
            module?.Dispose();
        }

        private TypeReference GetTypeReference(Type type) {
            return module.ImportReference(type);
        }

        #region String Types

        [Test]
        [TestCase(typeof(string))]
        [TestCase(typeof(char))]
        [TestCase(typeof(Guid))]
        [TestCase(typeof(DateTime))]
        [TestCase(typeof(DateTimeOffset))]
        [TestCase(typeof(DateOnly))]
        [TestCase(typeof(TimeOnly))]
        public void GetTSPropertyType_StringTypes_ReturnsStringType(Type stringType) {
            // Arrange
            var typeRef = GetTypeReference(stringType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.String));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("''"));
        }

        [Test]
        public void GetTSPropertyType_String_WithNullableStringsOption_ReturnsNullableString() {
            // Arrange
            var typeRef = GetTypeReference(typeof(string));
            var optionsWithNullableStrings = new Options { UseNullableStrings = true };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithNullableStrings);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.String));
            Assert.That(result.IsNullable, Is.True);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("null"));
        }

        [Test]
        public void GetTSPropertyType_ByteArray_ReturnsStringType() {
            // Arrange - byte[] is serialized as a base64 string in JSON, not a number array
            var typeRef = GetTypeReference(typeof(byte[]));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.String));
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
        }

        [Test]
        public void GetTSPropertyType_ByteArrayList_ReturnsStringCollection() {
            // Arrange
            var typeRef = GetTypeReference(typeof(List<byte[]>));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.String));
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
        }

        [Test]
        public void GetTSPropertyType_JaggedByteArray_ReturnsStringCollection() {
            // Arrange - only the inner byte[] is base64; the outer array stays a collection
            var typeRef = GetTypeReference(typeof(byte[][]));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.String));
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
        }

        [Test]
        public void GetTSPropertyType_ByteList_ReturnsNumberCollection() {
            // Arrange - only byte[] is base64; other byte collections serialize as number arrays
            var typeRef = GetTypeReference(typeof(List<byte>));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Number));
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
        }

        #endregion

        #region Number Types

        [Test]
        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(byte))]
        [TestCase(typeof(sbyte))]
        [TestCase(typeof(uint))]
        [TestCase(typeof(ulong))]
        [TestCase(typeof(ushort))]
        [TestCase(typeof(float))]
        [TestCase(typeof(double))]
        [TestCase(typeof(decimal))]
        public void GetTSPropertyType_NumericTypes_ReturnsNumberType(Type numericType) {
            // Arrange
            var typeRef = GetTypeReference(numericType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Number));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("0"));
        }

        #endregion

        #region Boolean Type

        [Test]
        public void GetTSPropertyType_Boolean_ReturnsBooleanType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(bool));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Boolean));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("false"));
        }

        #endregion

        #region Enum Types

        [Test]
        public void GetTSPropertyType_EnumType_ReturnsEnumType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(DayOfWeek));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(nameof(DayOfWeek)));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.True);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            //Assert.That(result.GetDefaultValue(), Is.EqualTo());
        }

        [Test]
        public void GetTSPropertyType_NullableEnum_ReturnsNullableEnumType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(DayOfWeek?));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("DayOfWeek"));
            Assert.That(result.IsNullable, Is.True);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.True);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("null"));
        }

        #endregion

        #region Void Types

        [Test]
        [TestCase(typeof(void))]
        [TestCase(typeof(Task))]
        public void GetTSPropertyType_VoidTypes_ReturnsVoidType(Type voidType) {
            // Arrange
            var typeRef = GetTypeReference(voidType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Void));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
        }

        #endregion

        #region Nullable Types

        [Test]
        [TestCase(typeof(int?), TSTypeConsts.Number)]
        [TestCase(typeof(bool?), TSTypeConsts.Boolean)]
        [TestCase(typeof(DateTime?), TSTypeConsts.String)]
        public void GetTSPropertyType_NullableTypes_ReturnsNullableType(Type nullableType, string expectedTypeName) {
            // Arrange
            var typeRef = GetTypeReference(nullableType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(expectedTypeName));
            Assert.That(result.IsNullable, Is.True);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("null"));
        }

        #endregion

        #region Collection Types

        [Test]
        [TestCase(typeof(int[]), TSTypeConsts.Number, 1)]
        [TestCase(typeof(int[][]), TSTypeConsts.Number, 2)]
        public void GetTSPropertyType_Arrays_ReturnsCollectionType(Type arrayType, string expectedTypeName, int expectedJaggedCount) {
            // Arrange
            var typeRef = GetTypeReference(arrayType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(expectedTypeName));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(expectedJaggedCount));
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("[]"));
        }

        [Test]
        [TestCase(typeof(List<string>), TSTypeConsts.String)]
        [TestCase(typeof(IEnumerable<int>), TSTypeConsts.Number)]
        [TestCase(typeof(HashSet<bool>), TSTypeConsts.Boolean)]
        public void GetTSPropertyType_GenericCollections_ReturnsCollectionType(Type collectionType, string expectedTypeName) {
            // Arrange
            var typeRef = GetTypeReference(collectionType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(expectedTypeName));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("[]"));
        }

        #endregion

        #region Dictionary Types

        [Test]
        [TestCase(typeof(Dictionary<string, int>), TSTypeConsts.Number)]
        [TestCase(typeof(IDictionary<string, string>), TSTypeConsts.String)]
        public void GetTSPropertyType_Dictionaries_ReturnsDictionaryType(Type dictionaryType, string expectedValueTypeName) {
            // Arrange
            var typeRef = GetTypeReference(dictionaryType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(expectedValueTypeName));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.True);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{}"));
        }

        [Test]
        public void GetTSPropertyType_DictionaryWithArrayValue_ReturnsDictionaryAndCollection() {
            // Arrange
            var typeRef = GetTypeReference(typeof(Dictionary<string, int[]>));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Number));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
            Assert.That(result.IsDictionary, Is.True);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{}"));
        }

        #endregion

        #region File Types

        [Test]
        public void GetTSPropertyType_IFormFile_ReturnsFileType() {
            // Arrange
            var typeRef = module.ImportReference(typeof(Microsoft.AspNetCore.Http.IFormFile));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.File));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{} as File"));
        }

        [Test]
        public void GetTSPropertyType_IFormFileCollection_ReturnsFileCollectionType() {
            // Arrange
            var typeRef = module.ImportReference(typeof(Microsoft.AspNetCore.Http.IFormFileCollection));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.File));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.True);
            Assert.That(result.JaggedCount, Is.EqualTo(1));
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("[]"));
        }

        #endregion

        #region FormData Types

        [Test]
        public void GetTSPropertyType_IFormCollection_ReturnsFormDataType() {
            // Arrange
            var typeRef = module.ImportReference(typeof(Microsoft.AspNetCore.Http.IFormCollection));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.FormData));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{} as FormData"));
        }

        #endregion

        #region Unknown Types

        [Test]
        public void GetTSPropertyType_JsonElement_ReturnsUnknownType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(System.Text.Json.JsonElement));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Unknown));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{} as unknown"));
        }

        #endregion

        #region Generic Types

        [Test]
        [TestCase(typeof(Task<string>), TSTypeConsts.String)]
        [TestCase(typeof(Task<int>), TSTypeConsts.Number)]
        public void GetTSPropertyType_TaskOfT_ExtractsGenericAndReturnsCorrectType(Type taskType, string expectedTypeName) {
            // Arrange
            var typeRef = GetTypeReference(taskType);

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(expectedTypeName));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
        }

        #endregion

        #region Custom Object Types

        [Test]
        public void GetTSPropertyType_CustomClass_ReturnsObjectType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(TSTypeMapperTests));

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("TSTypeMapperTests"));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.JaggedCount, Is.Zero);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
            Assert.That(result.GenericArguments.Count(), Is.Zero);
            Assert.That(result.GetDefaultValue(), Is.EqualTo("{} as TSTypeMapperTests"));
        }

        #endregion

        #region Import Handler

        [Test]
        [TestCase(typeof(TSTypeMapperTests), true)]
        [TestCase(typeof(int), false)]
        public void GetTSPropertyType_ImportHandler_CalledForCustomTypesOnly(Type type, bool shouldCallHandler) {
            // Arrange
            var typeRef = GetTypeReference(type);
            bool importHandlerCalled = false;

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, options, importHandler: (fullName, typeName) => {
                importHandlerCalled = true;
                return true;
            });

            // Assert
            Assert.That(importHandlerCalled, Is.EqualTo(shouldCallHandler));
        }

        #endregion

        #region Custom Type Mappings

        [Test]
        public void GetTSPropertyType_CustomMapping_ReturnsCustomType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(Uri));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(Uri).FullName!, "string" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("string"));
            Assert.That(result.IsNullable, Is.False);
            Assert.That(result.IsCollection, Is.False);
            Assert.That(result.IsDictionary, Is.False);
            Assert.That(result.IsEnum, Is.False);
        }

        [Test]
        public void GetTSPropertyType_CustomMapping_OverridesBuiltInType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(DateTime));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(DateTime).FullName!, "Date" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("Date"));
        }

        [Test]
        public void GetTSPropertyType_CustomMapping_WithCollection() {
            // Arrange
            var typeRef = GetTypeReference(typeof(List<Uri>));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(Uri).FullName!, "string" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("string"));
            Assert.That(result.IsCollection, Is.True);
        }

        [Test]
        public void GetTSPropertyType_CustomMapping_WithNullable() {
            // Arrange
            var typeRef = GetTypeReference(typeof(DateTime?));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(DateTime).FullName!, "Date" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("Date"));
            Assert.That(result.IsNullable, Is.True);
        }

        [Test]
        public void GetTSPropertyType_NoCustomMapping_FallsBackToDefault() {
            // Arrange
            var typeRef = GetTypeReference(typeof(int));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(Uri).FullName!, "string" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo(TSTypeConsts.Number));
        }

        [Test]
        public void GetTSPropertyType_CustomMapping_InlineObjectType() {
            // Arrange
            var typeRef = GetTypeReference(typeof(TSTypeMapperTests));
            var optionsWithMapping = new Options {
                CustomTypeMappings = new Dictionary<string, string> {
                    { typeof(TSTypeMapperTests).FullName!, "{ lat: number, lng: number }" }
                }
            };

            // Act
            var result = TSTypeMapper.GetTSPropertyType(typeRef, optionsWithMapping);

            // Assert
            Assert.That(result.TypeName, Is.EqualTo("{ lat: number, lng: number }"));
        }

        #endregion
    }
}