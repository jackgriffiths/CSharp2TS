using CSharp2TS.CLI.Generators.Common;
using CSharp2TS.CLI.Generators.TSEnums;
using CSharp2TS.CLI.Generators.TSInterfaces;
using CSharp2TS.CLI.Generators.TSServices;
using CSharp2TS.CLI.Generators.TSUnions;
using CSharp2TS.CLI.Templates;
using CSharp2TS.CLI.Utility;
using CSharp2TS.Core.Attributes;
using Mono.Cecil;

namespace CSharp2TS.CLI.Generators {
    public class Generator {
        private readonly Options options;
        private readonly Dictionary<string, TSFileInfo> files = [];
        private readonly HashSet<string> generatedInterfaces = [];

        public Generator(Options options) {
            this.options = options;
        }

        public void Run() {
            if (options.GenerateModels) {
                GenerateModels();
            }

            if (options.GenerateServices) {
                GenerateServices();
            }
        }

        private void GenerateModels() {
            if (Directory.Exists(options.ModelOutputFolder)) {
                Directory.Delete(options.ModelOutputFolder, true);
            }

            Directory.CreateDirectory(options.ModelOutputFolder!);

            // Gather all imports so we know if we can reference them later on
            foreach (var assemblyPath in options.ModelAssemblyPaths) {
                using (var assembly = LoadAssembly(assemblyPath)) {
                    GatherModelImports(assembly.MainModule);
                }
            }

            foreach (var assemblyPath in options.ModelAssemblyPaths) {
                using (var assembly = LoadAssembly(assemblyPath)) {
                    GenerateInterfaces(assembly.MainModule, options);
                    GenerateEnums(assembly.MainModule, options);
                }
            }
        }

        private void GatherModelImports(ModuleDefinition module) {
            var enums = GetTypesByAttribute(module, typeof(TSEnumAttribute));

            foreach (var type in enums) {
                files.Add(type.FullName, NameUtility.GetFileDetails(type, options, options.ModelOutputFolder!));
            }

            var interfaces = GetTypesByAttribute(module, typeof(TSInterfaceAttribute));

            foreach (var type in interfaces) {
                files.Add(type.FullName, NameUtility.GetFileDetails(type, options, options.ModelOutputFolder!));
            }

            foreach (var type in interfaces) {
                GatherDerivedTypes(type);
            }
        }

        private void GatherDerivedTypes(TypeDefinition rootType) {
            if (!JsonPolymorphismUtility.IsPolymorphicRoot(rootType)) {
                return;
            }

            // [JsonDerivedType] types are generated alongside their polymorphic root and default to its
            // folder. Types with their own [TSInterface] attribute are registered by the passes above.
            string rootFolder = files[rootType.FullName].Folder;

            foreach (var derivedType in JsonPolymorphismUtility.GetDerivedTypes(rootType)) {
                if (derivedType.Type.HasAttribute<TSInterfaceAttribute>() || files.ContainsKey(derivedType.Type.FullName)) {
                    continue;
                }

                files.Add(derivedType.Type.FullName, NameUtility.GetFileDetails(derivedType.Type, options, rootFolder));

                GatherDerivedTypes(derivedType.Type);
            }
        }

        private void GenerateServices() {
            if (Directory.Exists(options.ServicesOutputFolder)) {
                Directory.Delete(options.ServicesOutputFolder, true);
            }

            Directory.CreateDirectory(options.ServicesOutputFolder!);

            GenerateApiClient();

            foreach (var assemblyPath in options.ServicesAssemblyPaths) {
                using (var assembly = LoadAssembly(assemblyPath)) {
                    GatherModelImports(assembly.MainModule);
                }
            }

            foreach (var assemblyPath in options.ServicesAssemblyPaths) {
                using (var assembly = LoadAssembly(assemblyPath)) {
                    GenerateServices(assembly.MainModule, options);
                }
            }
        }

        private AssemblyDefinition LoadAssembly(string assemblyPath) {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);

            return AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters {
                AssemblyResolver = resolver,
            });
        }

        private void GenerateApiClient() {
            string apiClientTemplate = new TSAxiosApiClientTemplate().TransformText();
            string path = Path.Combine(options.ServicesOutputFolder!, "apiClient.ts");

            File.WriteAllText(path, apiClientTemplate);
        }

        private void GenerateInterfaces(ModuleDefinition module, Options options) {
            var types = GetTypesByAttribute(module, typeof(TSInterfaceAttribute));

            if (!types.Any()) {
                return;
            }

            TSInterfaceGenerator generator = new(files, options);
            TSUnionGenerator unionGenerator = new(files);

            foreach (TypeDefinition type in types) {
                GenerateInterface(generator, unionGenerator, type);
            }
        }

        private void GenerateInterface(TSInterfaceGenerator generator, TSUnionGenerator unionGenerator, TypeDefinition type) {
            if (!generatedInterfaces.Add(type.FullName)) {
                return;
            }

            if (JsonPolymorphismUtility.IsPolymorphicRoot(type)) {
                GenerateFile(files[type.FullName], unionGenerator.Generate(type));

                // Derived types without their own [TSInterface] attribute are generated with their root
                foreach (var derivedType in JsonPolymorphismUtility.GetDerivedTypes(type)) {
                    if (!derivedType.Type.HasAttribute<TSInterfaceAttribute>()) {
                        GenerateInterface(generator, unionGenerator, derivedType.Type);
                    }
                }
            } else {
                GenerateFile(files[type.FullName], generator.Generate(type));
            }
        }

        private void GenerateEnums(ModuleDefinition module, Options options) {
            var types = GetTypesByAttribute(module, typeof(TSEnumAttribute));

            if (!types.Any()) {
                return;
            }

            TSEnumGenerator generator = new();

            foreach (TypeDefinition type in types) {
                string fileContents = generator.Generate(type);

                GenerateFile(files[type.FullName], fileContents);
            }
        }

        private void GenerateServices(ModuleDefinition module, Options options) {
            var types = GetTypesByAttribute(module, typeof(TSServiceAttribute));

            foreach (var type in types) {
                files.Add(type.FullName, TSAxiosServiceGenerator.GetFileInfo(type, options));
            }

            var generator = new TSAxiosServiceGenerator(options, files);

            foreach (TypeDefinition type in types) {
                string fileContents = generator.Generate(type);

                GenerateFile(files[type.FullName], fileContents);
            }
        }

        private IEnumerable<TypeDefinition> GetTypesByAttribute(ModuleDefinition module, Type attributeType) {
            foreach (var type in module.GetTypes()) {
                if (type.HasAttribute(attributeType)) {
                    yield return type;
                }
            }
        }

        private void GenerateFile(TSFileInfo fileInfo, string fileContents) {
            if (!Directory.Exists(fileInfo.Folder)) {
                Directory.CreateDirectory(fileInfo.Folder);
            }

            if (File.Exists(fileInfo.FileFullPath)) {
                throw new InvalidOperationException($"File {fileInfo.FileFullPath} already exists.");
            }

            File.WriteAllText(fileInfo.FileFullPath, fileContents);
        }
    }
}
