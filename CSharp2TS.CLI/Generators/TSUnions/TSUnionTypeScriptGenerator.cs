using System.Text;

namespace CSharp2TS.CLI.Generators.TSUnions {
    public static class TSUnionTypeScriptGenerator {
        public static string Generate(TSUnion tsUnion) {
            StringBuilder sb = new();

            sb.AppendLine($"// Auto-generated from {tsUnion.Name}.cs");

            if (tsUnion.Imports.Count > 0) {
                sb.AppendLine();

                foreach (var item in tsUnion.Imports) {
                    sb.AppendLine($"import {item.Name} from '{item.Path}';");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"type {tsUnion.Name} = {string.Join(" | ", tsUnion.Members)};");
            sb.AppendLine();
            sb.AppendLine($"export default {tsUnion.Name};");

            return sb.ToString();
        }
    }
}
