using CSharp2TS.CLI;
using CSharp2TS.CLI.Generators.Common;
using CSharp2TS.CLI.Generators.TSInterfaces;
using CSharp2TS.CLI.Utility;
using CSharp2TS.Tests.Stubs.Enums;
using CSharp2TS.Tests.Stubs.Models;
using Mono.Cecil;

namespace CSharp2TS.Tests.Generators {
    public class TSInterfaceGeneratorTest : GeneratorTestBase {
        private ModuleDefinition module = null!;
        private TSInterfaceGenerator generator = null!;
        private TSInterfaceGenerator generatorPascalCase = null!;
        private Dictionary<string, TSFileInfo> files = null!;
        private Options options = null!;
        private Options optionsPascalCase = null!;

        [SetUp]
        public void Setup() {
            // Load the test assembly to get TypeReferences
            string assemblyPath = typeof(TSInterfaceGeneratorTest).Assembly.Location;
            module = ModuleDefinition.ReadModule(assemblyPath);

            // Setup options
            options = new Options {
                UseNullableStrings = false,
            };
            optionsPascalCase = new Options {
                UseNullableStrings = false,
                MemberNameCasingStyle = CasingStyle.PascalCase,
            };

            // Setup files dictionary - needed for import resolution
            files = [];

            // Add test types to files dictionary
            AddType(typeof(TestClass));
            AddType(typeof(TestClass2));
            AddType(typeof(ParentClass));
            AddType(typeof(ChildClass));
            AddType(typeof(TestRecord));
            AddType(typeof(GenericClass1<>));
            AddType(typeof(GenericClass2<,>));
            AddType(typeof(TestClassInFolder));
            AddType(typeof(TestEnum));
            AddType(typeof(TestEnumInFolder));
            AddType(typeof(TestClassWithStub));
            AddType(typeof(TestClassWithMethods));
            AddType(typeof(TestClassWithMethodsDefault));
            AddType(typeof(TestClassMethodReturnTypes));
            AddType(typeof(Shape));
            AddType(typeof(Circle));
            AddType(typeof(Square));
            AddType(typeof(DefaultDiscriminatorRoot));
            AddType(typeof(IntDiscriminatorChild));
            AddType(typeof(NoDiscriminatorChild));

            generator = new TSInterfaceGenerator(files, options);
            generatorPascalCase = new TSInterfaceGenerator(files, optionsPascalCase);
        }

        [TearDown]
        public void TearDown() {
            module?.Dispose();
        }

        [Test]
        public void InterfaceGenerator_Class_AllProperties() {
            var typeRef = module.ImportReference(typeof(TestClass));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClass.ts", result);
        }

        [Test]
        public void InterfaceGenerator_Class_AllProperties_PascalCase() {
            var typeRef = module.ImportReference(typeof(TestClass));


            string result = generatorPascalCase.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClassPascalCase.ts", result);
        }

        [Test]
        public void InterfaceGenerator_ChildClassWithInheritance() {
            var typeRef = module.ImportReference(typeof(ChildClass));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/ChildClass.ts", result);
        }

        /// <summary>
        /// This test checks of the generation adds duplicated attributes when a child class overrides properties from a parent class.
        /// </summary>
        [Test]
        public void InterfaceGenerator_ChildClassWithInheritanceOverride() {
            var typeRef = module.ImportReference(typeof(ChildClassOverride));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/ChildClassOverride.ts", result);
        }

        [Test]
        public void InterfaceGenerator_Record() {
            var typeRef = module.ImportReference(typeof(TestRecord));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestRecord.ts", result);
        }

        [Test]
        public void InterfaceGenerator_GenericClassSingleParameter() {
            var typeRef = module.ImportReference(typeof(GenericClass1<>));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/GenericClass1.ts", result);
        }

        [Test]
        public void InterfaceGenerator_GenericClassMultipleParameters() {
            var typeRef = module.ImportReference(typeof(GenericClass2<,>));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/GenericClass2.ts", result);
        }

        [Test]
        public void InterfaceGenerator_StubGeneration() {
            var typeRef = module.ImportReference(typeof(TestClassWithStub));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClassWithStub.ts", result);
        }

        [Test]
        public void InterfaceGenerator_MethodGeneration() {
            var typeRef = module.ImportReference(typeof(TestClassWithMethods));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClassWithMethods.ts", result);
        }

        [Test]
        public void InterfaceGenerator_MethodGeneration_DefaultDisabled() {
            var typeRef = module.ImportReference(typeof(TestClassWithMethodsDefault));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClassWithMethodsDefault.ts", result);
        }

        [Test]
        public void InterfaceGenerator_MethodGeneration_ReturnTypes() {
            var typeRef = module.ImportReference(typeof(TestClassMethodReturnTypes));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/TestClassMethodReturnTypes.ts", result);
        }

        [Test]
        public void InterfaceGenerator_PolymorphicDerivedType_IncludesDiscriminatorAndInheritedProperties() {
            var typeRef = module.ImportReference(typeof(Circle));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/Circle.ts", result);
        }

        [Test]
        public void InterfaceGenerator_PolymorphicDerivedTypeWithTSInterfaceAttribute_UsesCustomName() {
            var typeRef = module.ImportReference(typeof(Square));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/SquareShape.ts", result);
        }

        [Test]
        public void InterfaceGenerator_PolymorphicDerivedType_DefaultDiscriminatorPropertyName() {
            var typeRef = module.ImportReference(typeof(DefaultDiscriminatorChild));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/DefaultDiscriminatorChild.ts", result);
        }

        [Test]
        public void InterfaceGenerator_PolymorphicDerivedType_IntDiscriminator() {
            var typeRef = module.ImportReference(typeof(IntDiscriminatorChild));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/IntDiscriminatorChild.ts", result);
        }

        [Test]
        public void InterfaceGenerator_PolymorphicDerivedType_NoDiscriminator() {
            var typeRef = module.ImportReference(typeof(NoDiscriminatorChild));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/NoDiscriminatorChild.ts", result);
        }

        private void AddType(Type type) {
            var typeRef = module.ImportReference(type);

            files[type.FullName!] = NameUtility.GetFileDetails(typeRef.Resolve(), options, "Models");
        }
    }
}