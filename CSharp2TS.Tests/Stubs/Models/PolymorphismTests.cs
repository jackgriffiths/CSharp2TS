using CSharp2TS.Core.Attributes;
using System.Text.Json.Serialization;

namespace CSharp2TS.Tests.Stubs.Models {
    [TSInterface]
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(Circle), "circle")]
    [JsonDerivedType(typeof(Square), "square")]
    public abstract class Shape {
        public int Id { get; set; }
    }

    public class Circle : Shape {
        public double Radius { get; set; }
    }

    [TSInterface("SquareShape", Folder = "Shapes")]
    public class Square : Shape {
        public double Width { get; set; }
    }

    [TSInterface]
    [JsonDerivedType(typeof(DefaultDiscriminatorChild), "child")]
    [JsonDerivedType(typeof(IntDiscriminatorChild), 2)]
    [JsonDerivedType(typeof(NoDiscriminatorChild))]
    public abstract class DefaultDiscriminatorRoot {
    }

    public class DefaultDiscriminatorChild : DefaultDiscriminatorRoot {
        public string Name { get; set; } = string.Empty;
    }

    public class IntDiscriminatorChild : DefaultDiscriminatorRoot {
        public int Value { get; set; }
    }

    public class NoDiscriminatorChild : DefaultDiscriminatorRoot {
        public bool Flag { get; set; }
    }

    [TSInterface]
    [JsonDerivedType(typeof(SelfReferencingRoot), "base")]
    public class SelfReferencingRoot {
    }
}
