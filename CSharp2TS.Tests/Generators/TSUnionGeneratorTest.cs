using CSharp2TS.CLI;
using CSharp2TS.CLI.Generators.Common;
using CSharp2TS.CLI.Generators.TSUnions;
using CSharp2TS.CLI.Utility;
using CSharp2TS.Tests.Stubs.Models;
using Mono.Cecil;

namespace CSharp2TS.Tests.Generators {
    public class TSUnionGeneratorTest : GeneratorTestBase {
        private ModuleDefinition module = null!;
        private TSUnionGenerator generator = null!;
        private Dictionary<string, TSFileInfo> files = null!;
        private Options options = null!;

        [SetUp]
        public void Setup() {
            // Load the test assembly to get TypeReferences
            string assemblyPath = typeof(TSUnionGeneratorTest).Assembly.Location;
            module = ModuleDefinition.ReadModule(assemblyPath);

            options = new Options {
                UseNullableStrings = false,
            };

            // Setup files dictionary - needed for import resolution
            files = [];

            AddType(typeof(Shape));
            AddType(typeof(Circle));
            AddType(typeof(Square));
            AddType(typeof(DefaultDiscriminatorRoot));
            AddType(typeof(DefaultDiscriminatorChild));
            AddType(typeof(IntDiscriminatorChild));
            AddType(typeof(NoDiscriminatorChild));

            generator = new TSUnionGenerator(files);
        }

        [TearDown]
        public void TearDown() {
            module?.Dispose();
        }

        [Test]
        public void UnionGenerator_PolymorphicRoot_GeneratesUnionOfDerivedTypes() {
            var typeRef = module.ImportReference(typeof(Shape));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/Shape.ts", result);
        }

        [Test]
        public void UnionGenerator_MixedDiscriminators_GeneratesUnionOfDerivedTypes() {
            var typeRef = module.ImportReference(typeof(DefaultDiscriminatorRoot));

            string result = generator.Generate(typeRef.Resolve());

            TestMatchesFile("Expected/DefaultDiscriminatorRoot.ts", result);
        }

        [Test]
        public void UnionGenerator_SelfReferencingRoot_Throws() {
            var typeRef = module.ImportReference(typeof(SelfReferencingRoot));

            Assert.Throws<NotSupportedException>(() => generator.Generate(typeRef.Resolve()));
        }

        private void AddType(Type type) {
            var typeRef = module.ImportReference(type);

            files[type.FullName!] = NameUtility.GetFileDetails(typeRef.Resolve(), options, "Models");
        }
    }
}
