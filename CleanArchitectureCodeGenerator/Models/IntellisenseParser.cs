using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.Models
{
    /// <summary>
    /// Static class responsible for parsing C# files and extracting relevant 
    /// information about enums, classes, and their properties for Intellisense purposes.
    /// </summary>
    public static class IntellisenseParser
    {
        private static readonly string DefaultModuleName = "";
        private static readonly Regex IsNumber = new Regex("^[0-9a-fx]+[ul]{0,2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Processes the given C# file and extracts Intellisense objects representing
        /// enums and classes, including their properties and summaries.
        /// </summary>
        /// <param name="filePath">The path to the C# file to process.</param>
        /// <returns>A collection of IntellisenseObject instances, or null if the file doesn't exist or contains invalid C# syntax.</returns>
        internal static IEnumerable<IntellisenseObject> ProcessFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            if (root == null)
            {
                // If root is null, return early
                Console.WriteLine("Root is null. File might not have valid C# syntax.");
                return null;
            }

            var list = new List<IntellisenseObject>();

            // Process traditional namespaces
            foreach (var namespaceDecl in root.Members.OfType<NamespaceDeclarationSyntax>())
            {
                ProcessNamespace(namespaceDecl, list);
            }

            // Process file-scoped namespaces or global members directly under the root
            foreach (var member in root.Members)
            {
                ProcessRootMember(member, list);
            }
            return new HashSet<IntellisenseObject>(list);
        }

        /// <summary>
        /// Processes a namespace declaration and its members, adding them to the provided list.
        /// </summary>
        private static void ProcessNamespace(NamespaceDeclarationSyntax namespaceDecl, List<IntellisenseObject> list)
        {
            foreach (var member in namespaceDecl.Members)
            {
                list.AddRange(ProcessMember(member, namespaceDecl.Name.ToString()));
            }
        }

        /// <summary>
        /// Processes a member directly under the root (global or file-scoped namespace) and adds it to the provided list.
        /// </summary>
        private static void ProcessRootMember(MemberDeclarationSyntax member, List<IntellisenseObject> list)
        {
            if (member is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                foreach (var namespaceMember in fileScopedNamespace.Members)
                {
                    list.AddRange(ProcessMember(namespaceMember, fileScopedNamespace.Name.ToString()));
                }
            }
            else if (!(member is NamespaceDeclarationSyntax))
            {
                // Global level members, like classes, enums, etc., not within any namespace
                list.AddRange(ProcessMember(member, "Global"));
            }
        }

        /// <summary>
        /// Processes a member of a namespace or global scope, and returns a list of IntellisenseObject instances.
        /// </summary>
        private static List<IntellisenseObject> ProcessMember(MemberDeclarationSyntax member, string namespaceName)
        {
            if (member is EnumDeclarationSyntax enumDecl)
            {
                return ProcessEnum(enumDecl, namespaceName);
            }
            else if (member is ClassDeclarationSyntax classDecl)
            {
                return ProcessClass(classDecl, namespaceName);
            }
            return new List<IntellisenseObject>();
        }

        /// <summary>
        /// Processes an enum declaration, extracting its name, properties, and summary.
        /// </summary>
        private static List<IntellisenseObject> ProcessEnum(EnumDeclarationSyntax enumDecl, string namespaceName)
        {
            var data = new IntellisenseObject
            {
                Name = enumDecl.Identifier.Text,
                IsEnum = true,
                FullName = $"{namespaceName}.{enumDecl.Identifier.Text}",
                Namespace = namespaceName,
                Summary = GetSummary(enumDecl)
            };

            foreach (var member in enumDecl.Members)
            {
                var prop = new IntellisenseProperty
                {
                    Name = member.Identifier.Text,
                    Summary = GetSummary(member),
                    InitExpression = member.EqualsValue?.Value.ToString()
                };
                data.Properties.Add(prop);
            }

            return data.Properties.Count > 0 ? new List<IntellisenseObject> { data } : new List<IntellisenseObject>();
        }

        /// <summary>
        /// Processes a class declaration, extracting its name, properties, and summary.
        /// </summary>
        private static List<IntellisenseObject> ProcessClass(ClassDeclarationSyntax classDecl, string namespaceName)
        {
            var list = new List<IntellisenseObject>();

            // Determine the base class if any
            string baseName = null;
            string baseNamespace = null;

            if (classDecl.BaseList != null && classDecl.BaseList.Types.Count > 0)
            {
                var baseTypeSyntax = classDecl.BaseList.Types.FirstOrDefault();
                if (baseTypeSyntax != null)
                {
                    var baseTypeName = baseTypeSyntax.Type.ToString();
                    baseName = baseTypeName.Split('.').Last();  // Extracting the class name
                    baseNamespace = string.Join('.', baseTypeName.Split('.').Reverse().Skip(1).Reverse());  // Extracting the namespace if any
                }
            }

            var data = new IntellisenseObject
            {
                Name = classDecl.Identifier.Text,
                FullName = $"{namespaceName}.{classDecl.Identifier.Text}",
                Namespace = namespaceName,
                Summary = GetSummary(classDecl),
                BaseName = baseName,
                BaseNamespace = baseNamespace
            };

            foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
            {
                var prop = new IntellisenseProperty
                {
                    Name = member.Identifier.Text,
                    Type = GetIntellisenseType(member.Type),
                    Summary = GetSummary(member)
                };

                data.Properties.Add(prop);
            }

            list.Add(data);
            return list;
        }

        /// <summary>
        /// Extracts the summary comment from the leading trivia of the given syntax node.
        /// </summary>
        private static string GetSummary(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia().ToString();
            if (string.IsNullOrWhiteSpace(trivia))
            {
                return null;
            }

            // Extract summary comments if present
            var summaryMatch = Regex.Match(trivia, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
            return summaryMatch.Success ? summaryMatch.Groups[1].Value.Trim() : trivia.Trim();
        }

        /// <summary>
        /// Converts a TypeSyntax instance to an IntellisenseType, extracting relevant information.
        /// </summary>
        private static IntellisenseType GetIntellisenseType(TypeSyntax typeSyntax)
        {
            return new IntellisenseType
            {
                CodeName = typeSyntax.ToString(),
                IsArray = typeSyntax is ArrayTypeSyntax,
                IsDictionary = typeSyntax is GenericNameSyntax genericName &&
                                (genericName.Identifier.Text == "Dictionary" || genericName.Identifier.Text == "IDictionary")
            };
        }
    }
}
