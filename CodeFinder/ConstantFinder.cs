using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CodeFinder
{
    class ConstantInfo
    {
        public string ConstantName { get; set; }
        public string CoreNamespace { get; set; }
        public string ClassName { get; set; }
    }

    class UsageInfo: ConstantInfo
    {
        public string FilePath { get; set; }
        public int Count { get; set; }
    }

    internal class ConstantFinder
    {
        public static void Run(string coreCodePath, string usageCodePath)
        {

            var coreConstants = AnalyzeCoreCode(coreCodePath);
            var constantUsages = AnalyzeUsageCode(coreConstants, usageCodePath);

            var uniqueConstants = new List<UsageInfo>();
            foreach (var item in constantUsages)
            {
                var constantFound = uniqueConstants.FirstOrDefault(d => d.ConstantName == item.ConstantName && d.CoreNamespace == item.CoreNamespace && d.ClassName == item.ClassName);
                if (constantFound == null)
                    uniqueConstants.Add(item);
                else
                    constantFound.Count++;
            }
            foreach (var constantItem in uniqueConstants)
            {
                File.AppendAllText(usageCodePath.TrimEnd('\\') + @"\Constants_" + DateTime.Now.Hour + ".txt", $"{constantItem.CoreNamespace}\t{constantItem.ClassName}\t{constantItem.ConstantName}\t{constantItem.Count}\t{constantItem.FilePath}\n");
            }
            Console.WriteLine("Constants finder complete");

        }

        static List<ConstantInfo> AnalyzeCoreCode(string directoryPath)
        {
            var constants = new List<ConstantInfo>();

            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            foreach (var csFile in csFiles)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(csFile));
                var root = syntaxTree.GetRoot();

                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

                foreach (var classDeclaration in classDeclarations)
                {
                    var namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    var namespaceName = namespaceDeclaration?.Name.ToString();

                    var className = classDeclaration.Identifier.ValueText;

                    var constantDeclarations = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                        .Where(field => field.Modifiers.Any(modifier => modifier.ValueText == "const"));

                    foreach (var constantDeclaration in constantDeclarations)
                    {
                        foreach (var variable in constantDeclaration.Declaration.Variables)
                        {
                            var constantInfo = new ConstantInfo
                            {
                                ConstantName = variable.Identifier.ValueText,
                                ClassName = className,
                                CoreNamespace = namespaceName
                            };
                            constants.Add(constantInfo);
                        }
                    }
                }
            }

            return constants;
        }

        static List<UsageInfo> AnalyzeUsageCode(List<ConstantInfo> coreConstants, string directoryPath)
        {
            var usageInfos = new List<UsageInfo>();

            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            foreach (var csFile in csFiles)
            {
                var fileContents = File.ReadAllText(csFile);

                foreach (var constant in coreConstants)
                {
                    var constantFullName = $"{constant.ClassName}.{constant.ConstantName}";
                    var usageIndices = FindAllIndices(fileContents, constantFullName);

                    foreach (var index in usageIndices)
                    {
                        var usageInfo = new UsageInfo
                        {
                            ConstantName = constantFullName,
                            FilePath = csFile,
                            CoreNamespace = constant.CoreNamespace,
                            ClassName = constant.ClassName
                        };
                        usageInfos.Add(usageInfo);
                    }
                }
            }

            return usageInfos;
        }

        static List<int> FindAllIndices(string source, string searchText)
        {
            var indices = new List<int>();
            int index = source.IndexOf(searchText);
            while (index != -1)
            {
                indices.Add(index);
                index = source.IndexOf(searchText, index + 1);
            }
            return indices;
        }
    }
}
