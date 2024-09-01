using CleanArchitecture.CodeGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public class CSharpSyntaxParser
    {
        public IEnumerable<CSharpClassObject> ProcessFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            if (root == null)
            {
                Console.WriteLine("Root is null. File might not have valid C# syntax.");
                return null;
            }


            var list = new List<CSharpClassObject>();

            foreach (var namespaceDecl in root.Members.OfType<NamespaceDeclarationSyntax>())
            {
                ProcessNamespace(namespaceDecl, list);
            }

            foreach (var member in root.Members)
            {
                ProcessRootMember(member, list);
            }
            return new HashSet<CSharpClassObject>(list);
        }

        private void ProcessNamespace(NamespaceDeclarationSyntax namespaceDecl, List<CSharpClassObject> list)
        {
            foreach (var member in namespaceDecl.Members)
            {
                list.AddRange(ProcessMember(member, namespaceDecl.Name.ToString()));
            }
        }

        private void ProcessRootMember(MemberDeclarationSyntax member, List<CSharpClassObject> list)
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
                list.AddRange(ProcessMember(member, "Global"));
            }
        }

        private List<CSharpClassObject> ProcessMember(MemberDeclarationSyntax member, string namespaceName)
        {
            if (member is EnumDeclarationSyntax enumDecl)
            {
                return ProcessEnum(enumDecl, namespaceName);
            }
            else if (member is ClassDeclarationSyntax classDecl)
            {
                return ProcessClass(classDecl, namespaceName);
            }
            return new List<CSharpClassObject>();
        }

        private List<CSharpClassObject> ProcessEnum(EnumDeclarationSyntax enumDecl, string namespaceName)
        {
            var data = new CSharpClassObject
            {
                Name = enumDecl.Identifier.Text,
                IsEnum = true,
                FullName = $"{namespaceName}.{enumDecl.Identifier.Text}",
                Namespace = namespaceName,
                Summary = GetSummary(enumDecl)
            };

            foreach (var member in enumDecl.Members)
            {
                var prop = new ClassProperty
                {
                    PropertyName = member.Identifier.Text,
                    Summary = GetSummary(member),
                    InitExpression = member.EqualsValue?.Value.ToString()
                };
                data.Properties.Add(prop);
            }

            return data.Properties.Count > 0 ? new List<CSharpClassObject> { data } : new List<CSharpClassObject>();
        }

        private List<CSharpClassObject> ProcessClass(ClassDeclarationSyntax classDecl, string namespaceName)
        {
            var list = new List<CSharpClassObject>();

            string baseName = null;
            string baseNamespace = null;

            if (classDecl.BaseList != null && classDecl.BaseList.Types.Count > 0)
            {
                var baseTypeSyntax = classDecl.BaseList.Types.FirstOrDefault();
                if (baseTypeSyntax != null)
                {
                    var baseTypeName = baseTypeSyntax.Type.ToString();
                    baseName = baseTypeName.Split('.').Last();
                    baseNamespace = string.Join('.', baseTypeName.Split('.').Reverse().Skip(1).Reverse());
                }
            }

            var data = new CSharpClassObject
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
                var prop = new ClassProperty
                {
                    PropertyName = member.Identifier.Text,
                    Type = GetType(member.Type),
                    Summary = GetSummary(member),
                    propertyDeclarationSyntax = member
                };

                data.Properties.Add(prop);
            }

            list.Add(data);
            return list;
        }

        private string GetSummary(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia().ToString();
            if (string.IsNullOrWhiteSpace(trivia))
            {
                return null;
            }

            var summaryMatch = Regex.Match(trivia, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
            return summaryMatch.Success ? summaryMatch.Groups[1].Value.Trim() : trivia.Trim();
        }

        private PropertyType GetType(TypeSyntax typeSyntax)
        {

            bool isNullable = typeSyntax is NullableTypeSyntax;
            bool isKnownType = IsKnownType(typeSyntax);
            bool isKnownBaseType = IsKnownBaseType(typeSyntax);

            var type = new PropertyType
            {
                TypeName = typeSyntax.ToString(),
                IsArray = typeSyntax is ArrayTypeSyntax,
                IsList = typeSyntax is GenericNameSyntax genericName &&
                                (genericName.Identifier.Text == "List" || genericName.Identifier.Text == "IList"),
                IsDictionary = typeSyntax is GenericNameSyntax genericNameDict &&
                                (genericNameDict.Identifier.Text == "Dictionary" || genericNameDict.Identifier.Text == "IDictionary"),
                IsNullable = isNullable,
                // The IsKnown property is true if both IsKnownType and IsKnownBaseType are true.
                // IsKnown = isKnownType && isKnownBaseType,
                IsKnownType = isKnownType,
                // IsKnownBaseType = isKnownBaseType
            };
            return type;
        }

        // This method checks if the type is a known type (primitive types, base classes, etc.)
        private bool IsKnownType(TypeSyntax typeSyntax)
        {
            var knownPrimitiveTypes = new HashSet<string>
                {
                    // Represents a 32-bit signed integer.
                    "int",    // Maps to `int` in the database (SQL Server).

                    // Nullable 32-bit signed integer.
                    "int?",   // Maps to `int` in the database with nullability.

                    // Represents a 64-bit signed integer.
                    "long",   // Maps to `bigint` in the database.

                    // Nullable 64-bit signed integer.
                    "long?",  // Maps to `bigint` in the database with nullability.

                    // Represents a 16-bit signed integer.
                    "short",  // Maps to `smallint` in the database.

                    // Nullable 16-bit signed integer.
                    "short?", // Maps to `smallint` in the database with nullability.

                    // Represents an 8-bit unsigned integer.
                    "byte",   // Maps to `tinyint` in the database.

                    // Nullable 8-bit unsigned integer.
                    "byte?",  // Maps to `tinyint` in the database with nullability.

                    // Represents a single-precision floating-point number.
                    "float",  // Maps to `real` in the database.

                    // Nullable single-precision floating-point number.
                    "float?", // Maps to `real` in the database with nullability.

                    // Represents a double-precision floating-point number.
                    "double", // Maps to `float` in the database.

                    // Nullable double-precision floating-point number.
                    "double?",// Maps to `float` in the database with nullability.

                    // Represents a decimal number with higher precision.
                    "decimal",// Maps to `decimal(18, 2)` in the database, with customizable precision and scale.

                    // Nullable decimal number with higher precision.
                    "decimal?", // Maps to `decimal(18, 2)` in the database with nullability.

                    // Represents a Boolean value (true or false).
                    "bool",   // Maps to `bit` in the database.

                    // Nullable Boolean value.
                    "bool?",  // Maps to `bit` in the database with nullability.

                    // Represents a single Unicode character.
                    "char",   // Maps to `nchar(1)` in the database.

                    // Nullable Unicode character.
                    "char?",  // Maps to `nchar(1)` in the database with nullability.

                    // Represents a sequence of Unicode characters (a string).
                    "string", // Maps to `nvarchar(max)` in the database by default. Length can be specified.

                    // Nullable string.
                    "string?",// Maps to `nvarchar(max)` in the database with nullability. Length can be specified.

                    // Represents a date and time.
                    "DateTime", // Maps to `datetime2` in the database, providing higher precision.

                    // Nullable date and time.
                    "DateTime?", // Maps to `datetime2` in the database with nullability.

                    // Represents a globally unique identifier.
                    "Guid", // Maps to `uniqueidentifier` in the database.

                    // Nullable globally unique identifier.
                    "Guid?", // Maps to `uniqueidentifier` in the database with nullability.
                };

            return knownPrimitiveTypes.Contains(typeSyntax.ToString());
        }

        // This method checks if the type is a known base type, similar to the base classes check in the original class
        private bool IsKnownBaseType(TypeSyntax typeSyntax)
        {
            var knownBaseTypes = new HashSet<string>
                {
                    "BaseAuditableSoftDeleteEntity", "BaseAuditableEntity", "BaseEntity", "IEntity", "ISoftDelete"
                };

            return knownBaseTypes.Contains(typeSyntax.ToString());
        }



    }
}
