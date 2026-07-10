using CSharp2TS.CLI.Generators.Entities;

namespace CSharp2TS.CLI.Generators.TSUnions {
    public class TSUnion {
        public string Name { get; private set; }
        public IList<TSImport> Imports { get; private set; } = [];
        public IList<string> Members { get; private set; } = [];

        public TSUnion(string name) {
            Name = name;
        }
    }
}
