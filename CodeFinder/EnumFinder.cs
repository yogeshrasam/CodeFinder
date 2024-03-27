using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFinder
{
    class EnumUsage
    {
        public string EnumName { get; set; }
        public string Namespace { get; set; }
        public string FilePath { get; set; }
        public int Count { get; set; }
    }

    internal static class EnumFinder
    {

        public static void Run(string coreCodePath, string usageCodePath)
        {

            List<EnumDeclarationSyntax> coreEnums = GetEnumsFromCodeFolder(coreCodePath);
            List<EnumUsage> enumUsages = GetEnumUsagesInCodeFolder(usageCodePath, coreEnums);
            var uniqueEnums = new List<EnumUsage>();
            foreach (var item in enumUsages)
            {
                var enumFound = uniqueEnums.FirstOrDefault(d => d.EnumName == item.EnumName && d.Namespace == item.Namespace);
                if (enumFound == null)
                    uniqueEnums.Add(item);
                else
                    enumFound.Count++;
            }
            foreach (var enumItem in uniqueEnums)
            {
                File.AppendAllText(usageCodePath.TrimEnd('\\') + @"\Enums_"+ DateTime.Now.Hour +".txt", $"{enumItem.Namespace}\t{enumItem.EnumName}\t{enumItem.Count}\t{enumItem.FilePath}\n");
            }
            Console.WriteLine("Enum finder complete");

        }


        static List<EnumDeclarationSyntax> GetEnumsFromCodeFolder(string folderPath)
        {
            List<EnumDeclarationSyntax> enums = new List<EnumDeclarationSyntax>();

            var syntaxTrees = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories)
                .Select(file => (file, tree: CSharpSyntaxTree.ParseText(File.ReadAllText(file))));

            foreach (var (_, syntaxTree) in syntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var enumDeclarations = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
                enums.AddRange(enumDeclarations);
            }

            return enums;
        }

        static List<EnumUsage> GetEnumUsagesInCodeFolder(string folderPath, List<EnumDeclarationSyntax> coreEnums)
        {
            List<EnumUsage> enumUsages = new List<EnumUsage>();

            var syntaxTrees = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories)
                .Select(file => (file, tree: CSharpSyntaxTree.ParseText(File.ReadAllText(file))));

            foreach (var (filePath, syntaxTree) in syntaxTrees)
            {
                var root = syntaxTree.GetRoot();

                var enumUsagesInFile = root.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Where(identifier => coreEnums.Any(coreEnum => coreEnum.Identifier.ValueText == identifier.Identifier.ValueText))
                    .Select(identifier =>
                    {
                        var parent = identifier.Parent;
                        string enumName = identifier.Identifier.ValueText;
                        string namespaceName = GetNamespace(coreEnums, enumName);
                        return new EnumUsage
                        {
                            EnumName = enumName,
                            Namespace = namespaceName,
                            FilePath = filePath
                        };
                    });

                enumUsages.AddRange(enumUsagesInFile);
            }

            return enumUsages;
        }

        static string GetNamespace(List<EnumDeclarationSyntax> coreEnums, string enumName)
        {
            var coreEnum = coreEnums.FirstOrDefault(core => core.Identifier.ValueText == enumName);
            if (coreEnum != null)
            {
                var namespaceDeclaration = coreEnum.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                return namespaceDeclaration?.Name.ToString();
            }
            return null;
        }
    }
}
