CSharp2TS.Core is a lightweight package that contains the attributes required for the **CSharp2TS.CLI** dotnet tool to generate TypeScript models and API services.

## Getting Started

Add the following attributes to your project to include or exclude items in the CSharp2TS.CLI tool's TypeScript generation.

**TSInterface** can be added to a class to generate a TypeScript interface.

```c#
[TSInterface]
public class TestModel {
    ...
}
```



**Polymorphic types** configured with System.Text.Json's `[JsonPolymorphic]` / `[JsonDerivedType]` attributes are generated as TypeScript discriminated unions. Add `TSInterface` to the polymorphic base type only - the derived types are generated automatically alongside it.

```c#
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

public class Square : Shape {
    public double Width { get; set; }
}
```

This generates an interface per derived type, each with its discriminator as a literal type, and a union type for the base:

```ts
// Circle.ts
interface Circle {
  kind: 'circle';
  radius: number;
  id: number;
}
```

```ts
// Shape.ts
import Circle from './Circle';
import Square from './Square';

type Shape = Circle | Square;
```

The discriminator property name follows `JsonPolymorphic.TypeDiscriminatorPropertyName` and defaults to `$type`, matching the serializer. String and int discriminators are supported. A derived type can also be given its own `TSInterface` attribute to customise its TypeScript name or folder.



**TSEnum** can be added to enums to generate a TypeScript enum.

```c#
[TSEnum]
public enum TestEnum {
    ...
}
```



**TSService** can be added to classes which inherit from `ControllerBase` to generate an api client.

```c#
[TSService]
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase {
    ...
}
```



**TSEndpoint** can be added to an API endpoint to specify / override the return type.

```c#
[HttpGet]
[TSEndpoint(typeof(string))]
public IActionResult Get() {
    return Ok("Hello World");
}
```



**TSExclude** can be added to properties and API endpoints to exclude it from the TypeScript generation.

```c#
[TSExclude]
public int ExcludedProperty { get; set; } // Property will not be included in the TypeScript file
```

```c#
[HttpGet]
[TSExclude] // Endpoint will not be included in the TypeScript file
public IActionResult Get() {
    return Ok("Hello World");
}
```



**TSNullable** can be added to properties to mark the type as nullable in the TypeScript generation.

```c#
[TSNullable]
public string NullableString { get; set; } // Produces nullableString: string | null
```



**TSImport** can be added to a controller to include custom TypeScript import statements in the generated service file. This is useful when an endpoint returns a custom type that is not a generated model.

```c#
[TSService]
[TSImport("CustomType", "../types/customType")]
[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase {
    [HttpGet]
    [TSEndpoint("CustomType")]
    public IActionResult Get() {
        return Ok(...);
    }
}
```

This generates the following import in the TypeScript service file:

```ts
import CustomType from '../types/customType';
```

Multiple `TSImport` attributes can be added to a single controller.



## Additional Options

**TypeName** can be passed to `TSInterface`, `TSEnum`, or `TSService` to override the generated TypeScript type name.

```c#
[TSInterface("MyCustomName")]
public class TestModel {
    ...
}
```

**Folder** can be set on `TSInterface`, `TSEnum`, or `TSService` to place the generated file in a subfolder of the output directory.

```c#
[TSInterface(Folder = "subfolder")]
public class TestModel {
    ...
}
```

**IncludeMethods** can be passed to `TSInterface` to include public methods in the generated TypeScript type.

```c#
[TSInterface(IncludeMethods = true)]
public class TestModel {
    public bool Test() {
        return true;
    }
    ...
}
```

**GenerateClass** can be set on `TSInterface` to also generate a function that returns a default instance of the interface.

```c#
[TSInterface(GenerateClass = true)]
public class TestModel {
    ...
}
```

**TSEndpoint with a string** can be used instead of a `Type` to specify a raw TypeScript return type.

```c#
[HttpGet]
[TSEndpoint("CustomType")]
public IActionResult Get() {
    return Ok(...);
}
```
