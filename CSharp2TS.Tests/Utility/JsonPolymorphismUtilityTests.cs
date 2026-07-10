using CSharp2TS.CLI.Utility;
using CSharp2TS.Tests.Stubs.Models;
using Mono.Cecil;

namespace CSharp2TS.Tests.Utility {
    public class JsonPolymorphismUtilityTests {
        private ModuleDefinition module = null!;

        [SetUp]
        public void Setup() {
            string assemblyPath = typeof(JsonPolymorphismUtilityTests).Assembly.Location;
            module = ModuleDefinition.ReadModule(assemblyPath);
        }

        [TearDown]
        public void TearDown() {
            module?.Dispose();
        }

        private TypeDefinition GetTypeDefinition(Type type) {
            return module.ImportReference(type).Resolve();
        }

        [Test]
        public void IsPolymorphicRoot_WithJsonDerivedTypes_ReturnsTrue() {
            Assert.That(JsonPolymorphismUtility.IsPolymorphicRoot(GetTypeDefinition(typeof(Shape))), Is.True);
        }

        [Test]
        public void IsPolymorphicRoot_WithoutJsonDerivedTypes_ReturnsFalse() {
            Assert.That(JsonPolymorphismUtility.IsPolymorphicRoot(GetTypeDefinition(typeof(TestClass))), Is.False);
        }

        [Test]
        public void GetDerivedTypes_ReturnsTypesAndDiscriminators() {
            var derivedTypes = JsonPolymorphismUtility.GetDerivedTypes(GetTypeDefinition(typeof(Shape)));

            Assert.That(derivedTypes, Has.Count.EqualTo(2));
            Assert.That(derivedTypes[0].Type.Name, Is.EqualTo(nameof(Circle)));
            Assert.That(derivedTypes[0].Discriminator, Is.EqualTo("circle"));
            Assert.That(derivedTypes[1].Type.Name, Is.EqualTo(nameof(Square)));
            Assert.That(derivedTypes[1].Discriminator, Is.EqualTo("square"));
        }

        [Test]
        public void GetDiscriminatorPropertyName_CustomName_ReturnsCustomName() {
            string name = JsonPolymorphismUtility.GetDiscriminatorPropertyName(GetTypeDefinition(typeof(Shape)));

            Assert.That(name, Is.EqualTo("kind"));
        }

        [Test]
        public void GetDiscriminatorPropertyName_NoJsonPolymorphicAttribute_ReturnsDefault() {
            string name = JsonPolymorphismUtility.GetDiscriminatorPropertyName(GetTypeDefinition(typeof(DefaultDiscriminatorRoot)));

            Assert.That(name, Is.EqualTo("$type"));
        }

        [Test]
        public void TryGetDiscriminator_StringDiscriminator_ReturnsStringLiteral() {
            bool result = JsonPolymorphismUtility.TryGetDiscriminator(GetTypeDefinition(typeof(Circle)), out string propertyName, out string tsLiteral);

            Assert.That(result, Is.True);
            Assert.That(propertyName, Is.EqualTo("kind"));
            Assert.That(tsLiteral, Is.EqualTo("'circle'"));
        }

        [Test]
        public void TryGetDiscriminator_IntDiscriminator_ReturnsNumberLiteral() {
            bool result = JsonPolymorphismUtility.TryGetDiscriminator(GetTypeDefinition(typeof(IntDiscriminatorChild)), out string propertyName, out string tsLiteral);

            Assert.That(result, Is.True);
            Assert.That(propertyName, Is.EqualTo("$type"));
            Assert.That(tsLiteral, Is.EqualTo("2"));
        }

        [Test]
        public void TryGetDiscriminator_NoDiscriminator_ReturnsFalse() {
            bool result = JsonPolymorphismUtility.TryGetDiscriminator(GetTypeDefinition(typeof(NoDiscriminatorChild)), out _, out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetDiscriminator_NotPartOfHierarchy_ReturnsFalse() {
            bool result = JsonPolymorphismUtility.TryGetDiscriminator(GetTypeDefinition(typeof(ChildClass)), out _, out _);

            Assert.That(result, Is.False);
        }
    }
}
