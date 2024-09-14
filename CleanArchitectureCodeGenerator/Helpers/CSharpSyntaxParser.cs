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
                   // InitExpression = member.EqualsValue?.Value.ToString()
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
                    Type = GetType(member),
                    Summary = GetSummary(member),
                    propertyDeclarationSyntax = member
                };

                   
                // Assign DisplayName and Description based on attributes
                foreach (var attributeListSyntax in member.AttributeLists)
                {
                    foreach (var attribute in attributeListSyntax.Attributes)
                    {

                        if (attribute.Name.ToString().Contains("Display"))
                        {
                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                            if (argument != null)
                            {
                                prop.DisplayName = argument.ToString().Replace("\"", "").Replace("Name = ", ""); // Assign the DisplayName
                            }
                        }


                        if (attribute.Name.ToString().Contains("Description"))
                        {
                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                            if (argument != null)
                            {
                                prop.Description = argument.ToString().Trim('"'); // Assign the Description
                            }
                        }

                        // Handle Scaffolding attributes
                        if (attribute.Name.ToString().Contains("Scaffolding"))
                        {
                            foreach (var argument in attribute.ArgumentList.Arguments)
                            {
                                var argumentName = argument.Expression.ToString();
                                if (argumentName.Contains("PropRole.Identifier"))
                                {
                                    prop.ScaffoldingAtt.PropRole = "Identifier";
                                }
                                else if (argumentName.Contains("PropRole.Searchable"))
                                {
                                    prop.ScaffoldingAtt.PropRole = "Searchable";
                                }
                                else if (argumentName.Contains("PropRole.Relationship"))
                                {
                                    prop.ScaffoldingAtt.PropRole = "Relationship";
                                }

                                if (argumentName.Contains("RelationshipType.OneToOne"))
                                {
                                    prop.ScaffoldingAtt.RelationshipType = "OneToOne";
                                }
                                else if (argumentName.Contains("RelationshipType.OneToMany"))
                                {
                                    prop.ScaffoldingAtt.RelationshipType = "OneToMany";
                                }
                                else if (argumentName.Contains("RelationshipType.ManyToOne"))
                                {
                                    prop.ScaffoldingAtt.RelationshipType = "ManyToOne";
                                }
                                else if (argumentName.Contains("RelationshipType.ManyToMany"))
                                {
                                    prop.ScaffoldingAtt.RelationshipType = "ManyToMany";
                                }

                                if (argumentName.Contains("DeleteBehavior.Cascade"))
                                {
                                    prop.ScaffoldingAtt.DeleteBehavior = "Cascade";
                                }
                                else if (argumentName.Contains("DeleteBehavior.Restrict"))
                                {
                                    prop.ScaffoldingAtt.DeleteBehavior = "Restrict";
                                }
                                else if (argumentName.Contains("DeleteBehavior.SetNull"))
                                {
                                    prop.ScaffoldingAtt.DeleteBehavior = "SetNull";
                                }
                                else if (argumentName.Contains("DeleteBehavior.NoAction"))
                                {
                                    prop.ScaffoldingAtt.DeleteBehavior = "NoAction";
                                }
                            }

                            // Assign InverseProperty value
                            var navPropertyArgument = attribute.ArgumentList.Arguments
                                .FirstOrDefault(arg => arg.ToString().Contains("inverseProperty"));
                            string _temp = "";
                            if (navPropertyArgument != null)
                            {
                                _temp = navPropertyArgument.ToString();
                                _temp = _temp.Replace("inverseProperty: \"", "");
                                _temp = _temp.Replace("\"", "");
                                prop.ScaffoldingAtt.InverseProperty = _temp;
                            }

                            //Assign ForeignKeyProperty value
                            var foreignKeyArgument = attribute.ArgumentList.Arguments
                                .FirstOrDefault(arg => arg.ToString().Contains("foreignKeyProperty"));

                            if (foreignKeyArgument != null)
                            {
                                _temp = foreignKeyArgument.ToString();
                                _temp = _temp.Replace("foreignKeyProperty: \"", "");
                                _temp = _temp.Replace("\"", "");
                                prop.ScaffoldingAtt.ForeignKeyProperty = _temp;
                            }

                            //Assign LinkingTable value
                            var linkingArgument = attribute.ArgumentList.Arguments
                            .FirstOrDefault(arg => arg.ToString().Contains("linkingTable"));

                            if (linkingArgument != null)
                            {
                                _temp = linkingArgument.ToString();
                                _temp = _temp.Replace("linkingTable: \"", "");
                                _temp = _temp.Replace("\"", "");
                                prop.ScaffoldingAtt.LinkingTable = _temp;
                            }

                        }


                    }
                }

                if (string.IsNullOrWhiteSpace(prop.DisplayName))
                {
                    prop.DisplayName = Utility.SplitCamelCase(member.Identifier.Text);
                }

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

        private PropertyType GetType(PropertyDeclarationSyntax propertyDeclaration)
        {
            TypeSyntax typeSyntax = propertyDeclaration.Type;

            bool isNullable = false;
            bool isKnownType = Utility.IsKnownType(typeSyntax);
            bool isKnownBaseType = Utility.IsKnownBaseType(typeSyntax);
            bool isDictionary = false;
            bool isList = false;
            bool isICollection = false;
            bool isIEnumerable = false;

            TypeSyntax actualTypeSyntax;
            // Check if it's a nullable type and get the underlying type if needed
            if (typeSyntax is NullableTypeSyntax nullableTypeSyntax)
            {
                actualTypeSyntax = nullableTypeSyntax.ElementType;
                isNullable = true;
            }
            else
            {
                actualTypeSyntax = typeSyntax;
            }

            if (actualTypeSyntax is GenericNameSyntax genericName)
            {
                var genericTypeName = genericName.Identifier.Text;
                isDictionary = (genericTypeName == "Dictionary" || genericTypeName == "IDictionary");
                isList = (genericTypeName == "List" || genericTypeName == "IList");
                isICollection = genericTypeName == "ICollection";
                isIEnumerable = genericTypeName == "IEnumerable";
            }

            var type = new PropertyType();
            type.TypeName = typeSyntax.ToString();
            type.IsArray = typeSyntax is ArrayTypeSyntax;
            type.IsList = isList;
            type.IsDictionary = isDictionary;
            type.IsNullable = isNullable;
            type.IsKnownType = isKnownType;
            type.IsICollection = isICollection;
            type.IsIEnumerable = isIEnumerable;
           // type.IsKnownBaseType = isKnownBaseType

            // Check for PropertyCategoryAttribute
            //if (propertyDeclaration != null)
            //{
            //    var attributeList = propertyDeclaration.AttributeLists;
            //    foreach (var attributeListSyntax in attributeList)
            //    {
            //        foreach (var attribute in attributeListSyntax.Attributes)
            //        {
            //            if (attribute.Name.ToString().Contains("Scaffolding"))
            //            {
            //                var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
            //                if (argument != null)
            //                {
            //                    var value = argument.ToString();
            //                    if (value.Contains("Identifier"))
            //                    {
            //                        type.IsIdentifier = true;
            //                    }
            //                    else if (value.Contains("Searchable"))
            //                    {
            //                        type.IsSearchable = true;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            return type;
        }
    }
}
