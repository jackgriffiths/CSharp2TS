using CSharp2TS.CLI.Generators.Common;
using CSharp2TS.CLI.Generators.Entities;
using CSharp2TS.CLI.Utility;
using Mono.Cecil;

namespace CSharp2TS.CLI.Generators.TSUnions {
    /// <summary>
    /// Generates a TypeScript union type for a polymorphic root, i.e. a [TSInterface] class
    /// with [JsonDerivedType] attributes. Each derived type is generated as its own interface,
    /// so the root itself becomes a union of its derived types.
    /// </summary>
    public class TSUnionGenerator {
        private readonly Dictionary<string, TSFileInfo> files;

        public TSUnionGenerator(Dictionary<string, TSFileInfo> files) {
            this.files = files;
        }

        public string Generate(TypeDefinition typeDef) {
            TSUnion tsUnion = new(NameUtility.GetName(typeDef));

            foreach (var derivedType in JsonPolymorphismUtility.GetDerivedTypes(typeDef)) {
                if (derivedType.Type.FullName == typeDef.FullName) {
                    throw new NotSupportedException(
                        $"{typeDef.FullName} declares itself as a derived type, which cannot be expressed " +
                        "as a TypeScript union. Move the base type's own serialization into a separate derived type.");
                }

                string memberName = NameUtility.GetName(derivedType.Type);

                tsUnion.Members.Add(memberName);
                TryAddTSImport(tsUnion, typeDef, derivedType.Type, memberName);
            }

            return BuildTsFile(tsUnion);
        }

        private void TryAddTSImport(TSUnion tsUnion, TypeDefinition typeDef, TypeDefinition target, string targetName) {
            if (tsUnion.Imports.Any(i => i.FullName == target.FullName)) {
                return;
            }

            var currentType = files[typeDef.FullName];

            if (!files.TryGetValue(target.FullName, out var targetType)) {
                return;
            }

            string importPath = currentType.GetImportPathTo(targetType);

            tsUnion.Imports.Add(new TSImport(target.FullName, targetName, importPath));
        }

        private string BuildTsFile(TSUnion tsUnion) {
            return TSUnionTypeScriptGenerator.Generate(tsUnion);
        }
    }
}
