using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFinder
{
    internal class Details
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FilePath { get; set; }
    }

    internal class Program
    {
        private static List<Details> enums = new List<Details>();
        private static List<Details> constants = new List<Details>();
        static void Main(string[] args)
        {
            //TestStack();
            string coreCodePath = @"C:\TFS\ici-platform\src";
            string usageCodePath = @"C:\TFS\ICM\Community\Plugin";

            ConstantFinder.Run(coreCodePath, usageCodePath);
            //EnumFinder.Run(coreCodePath, usageCodePath);
            //string pathToPlugin = @"C:\TFS\ICM\Community\Plugin";
            //string pathToAssemblies = @"C:\TFS\ici-platform\src\Packages";
            //FindUsage(pathToPlugin, pathToAssemblies);
            var maxVal = Max(1, 2);
            Console.WriteLine(maxVal);
        }

        public static int Max(int a, int b)
        {
            return a > a ? a : b;
        }

        private static void TestStack()
        {
            List<string> listA = new List<string> { "1", "2 " };
            List<string> listB = new List<string> { "1", "4 " };
            IEnumerable<string> list1 = new List<string>();

            for (int i = 0; i < 1000; i++)
            {
                IEnumerable<string> list2 = new List<string> { "5", "6" };

                list1 = list1.Concat(list2);

            }

            List<int> numbers = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                numbers.Add(i);
            }

            foreach (var group in numbers.MakeGroupsOf(10))
            {
                Console.WriteLine(group.Count());
            }

            numbers = new List<int>();
            foreach (var group in numbers.MakeGroupsOf(10))
            {
                Console.WriteLine(group);
            }
        }

        private static void FindUsage(string pathToPlugin, string pathToAssemblies)
        {
            // Find all .cs files in the directory and its subdirectories
            string[] csFiles = Directory.GetFiles(pathToPlugin, "*.cs", SearchOption.AllDirectories);

            List<MetadataReference> metadataReferences = new List<MetadataReference>();

            string[] dlls = Directory.GetFiles(pathToAssemblies, "Icertis.*.dll", SearchOption.AllDirectories);
            foreach (var file in dlls)
            {
                metadataReferences.Add(MetadataReference.CreateFromFile(file));
            }
            foreach (string filePath in csFiles)
            {
                if (filePath.Contains("\\App_Start\\") || filePath.Contains("AssemblyInfo"))
                {
                    continue;
                }
                // Process each .cs file
                ProcessCsFile(filePath, metadataReferences, pathToPlugin);
            }
            PrintToFile(pathToPlugin);
        }

        private static void PrintToFile(string pathToPlugin)
        {
            foreach (var enumItem in enums)
            {
                File.AppendAllText(pathToPlugin.TrimEnd('\\') + @"\Enums.txt", $"{enumItem.Name}\t{enumItem.Namespace}\t{enumItem.FilePath}\n");
            }

            foreach (var constantItem in constants)
            {
                File.AppendAllText(pathToPlugin.TrimEnd('\\') + @"\Constants.txt", $"Enum:'{constantItem.Name}' - '{constantItem.Namespace}' in {constantItem.FilePath}\n");
            }
        }

        private static void ProcessCsFile(string filePath, IList<MetadataReference> metadataReferences, string pathToPlugin)
        {
            // Read the content of the file
            string content = File.ReadAllText(filePath);

            // Use Roslyn to parse the content and analyze the syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(content);



            // Create a compilation with the assembly reference
            var compilation = CSharpCompilation.Create("TempCompilation")
                                               .AddReferences(metadataReferences)
                                               .AddSyntaxTrees(syntaxTree);

            // Get the semantic model for the syntax tree
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Find all identifier nodes (variables, methods, etc.) in the syntax tree
            var identifierNodes = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>();

            foreach (var identifierNode in identifierNodes)
            {
                // Check if the identifier is a reference to an Enum or Constant
                string identifier = identifierNode.Identifier.Text;

                // Get the symbol for the identifier from the semantic model
                var symbol = semanticModel.GetSymbolInfo(identifierNode).Symbol;

                // Check if the symbol is an Enum or Constant
                if (symbol?.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    if (namedTypeSymbol.TypeKind == TypeKind.Enum)
                    {
                        // Get the namespace and print the information
                        var namespaceName = namedTypeSymbol.ContainingNamespace.ToDisplayString();
                        if (!enums.Any(d => d.Name == identifier && d.Namespace == namespaceName))
                        {
                            enums.Add(new Details
                            {
                                Name = identifier,
                                Namespace = namespaceName,
                                FilePath = filePath
                            });
                        }
                        //File.AppendAllText(pathToPlugin.TrimEnd('\\')+ @"\Enums.txt", $"Enum:'{identifier}' - '{namespaceName}' in {filePath}\n");
                    }
                }
                else if (symbol?.Kind == SymbolKind.Field && symbol is IFieldSymbol fieldSymbol)
                {
                    if (fieldSymbol.IsConst)
                    {
                        // Get the namespace and print the information
                        var namespaceName = fieldSymbol.ContainingNamespace.ToDisplayString();
                        if (!constants.Any(d => d.Name == identifier && d.Namespace == namespaceName))
                        {
                            constants.Add(new Details
                            {
                                Name = identifier,
                                Namespace = namespaceName,
                                FilePath = filePath
                            });
                        }
                        //File.AppendAllText(pathToPlugin.TrimEnd('\\')+@"\Constants.txt", $"Constant: '{identifier}' - '{namespaceName}' in {filePath}\n");
                    }
                }
            }
        }
    }
}
