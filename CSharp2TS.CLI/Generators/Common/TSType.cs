using CSharp2TS.CLI.Generators.Entities;

namespace CSharp2TS.CLI.Generators.Common {
    public struct TSType {
        public TSType() {
        }

        public required string TypeName { get; init; }
        public bool IsNullable { get; init; }
        public bool IsDictionary { get; init; }
        public bool IsCollection { get; init; }
        public bool IsEnum { get; init; }
        // True when TypeName is a literal type, e.g. a polymorphic discriminator like 'circle' or 1
        public bool IsLiteral { get; init; }
        public int JaggedCount { get; init; }
        public IList<TSType> GenericArguments { get; init; } = [];

        public bool IsObject() {
            return !new[] { TSTypeConsts.String, TSTypeConsts.Number, TSTypeConsts.Boolean, TSTypeConsts.Void }.Contains(TypeName);
        }

        public string GetDefaultValue(string? defaultValue = null) {
            if (IsLiteral) {
                return TypeName;
            }

            if (IsDictionary) {
                return "{}";
            }

            if (IsCollection) {
                return "[]";
            }

            if (IsNullable) {
                return "null";
            }

            if (IsEnum) {
                return $"{TypeName}.{defaultValue}";
            }

            return TypeName switch {
                TSTypeConsts.String => "''",
                TSTypeConsts.Number => "0",
                TSTypeConsts.Boolean => "false",
                TSTypeConsts.Object => "new Object()",
                _ => "{} as " + TypeName + (GenericArguments.Count > 0 ? "<" + string.Join(", ", GenericArguments.Select(i => i.ToString())) + ">" : ""),
            };
        }

        public override string ToString() {
            string tsType = TypeName;

            if (IsNullable) {
                tsType += " | null";

                if (IsCollection || IsDictionary) {
                    tsType = $"({tsType})";
                }
            }

            if (IsCollection) {
                for (int i = 0; i < JaggedCount; i++) {
                    tsType += "[]";
                }
            }

            if (IsDictionary) {
                tsType = $"{{ [key: string]: {tsType} }}";
            }

            if (GenericArguments.Any()) {
                tsType += "<" + string.Join(", ", GenericArguments.Select(i => i.ToString())) + ">";
            }

            return tsType;
        }
    }
}
