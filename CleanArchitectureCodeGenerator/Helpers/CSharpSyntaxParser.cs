using CleanArchitecture.CodeGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public class CSharpSyntaxParser
    {
        public IEnumerable<CSharpClassObject> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            var compilationUnit = syntaxTreeRoot.GetRoot() as CompilationUnitSyntax;

            if (compilationUnit == null)
            {
                Console.WriteLine("Root is null. File might not have valid C# syntax.");
                return null;
            }


            var parsedClassObjects = new List<CSharpClassObject>();

            foreach (var namespaceDeclaration in compilationUnit.Members.OfType<NamespaceDeclarationSyntax>())
            {
                ParseNamespaceDeclaration(namespaceDeclaration, parsedClassObjects);
            }

            foreach (var member in compilationUnit.Members)
            {
                ParseGlobalMember(member, parsedClassObjects);
            }
            return new HashSet<CSharpClassObject>(parsedClassObjects);
        }

        private void ParseNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDecl, List<CSharpClassObject> list)
        {
            foreach (var member in namespaceDecl.Members)
            {
                list.AddRange(ParseMemberDeclaration(member, namespaceDecl.Name.ToString()));
            }
        }

        private void ParseGlobalMember(MemberDeclarationSyntax member, List<CSharpClassObject> list)
        {
            if (member is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                foreach (var namespaceMember in fileScopedNamespace.Members)
                {
                    list.AddRange(ParseMemberDeclaration(namespaceMember, fileScopedNamespace.Name.ToString()));
                }
            }
            else if (!(member is NamespaceDeclarationSyntax))
            {
                list.AddRange(ParseMemberDeclaration(member, "Global"));
            }
        }

        private List<CSharpClassObject> ParseMemberDeclaration(MemberDeclarationSyntax memberDeclaration, string namespaceName)
        {
            if (memberDeclaration is EnumDeclarationSyntax enumDeclaration)
            {
                return ParseEnumDeclaration(enumDeclaration, namespaceName);
            }
            else if (memberDeclaration is ClassDeclarationSyntax classDeclaration)
            {
                return ParseClassDeclaration(classDeclaration, namespaceName);
            }
            return new List<CSharpClassObject>();
        }

        private List<CSharpClassObject> ParseEnumDeclaration(EnumDeclarationSyntax enumDeclaration, string namespaceName)
        {
            var classObject = new CSharpClassObject
            {
                Name = enumDeclaration.Identifier.Text,
                IsEnumType = true,
                FullyQualifiedName = $"{namespaceName}.{enumDeclaration.Identifier.Text}",
                ClassNamespace = namespaceName,
                Summary = ExtractSummaryFromNode(enumDeclaration)
            };

            foreach (var member in enumDeclaration.Members)
            {
                var classProperty = new ClassProperty
                {
                    PropertyName = member.Identifier.Text,
                    Summary = ExtractSummaryFromNode(member),
                   // InitExpression = member.EqualsValue?.Value.ToString()
                };
                classObject.ClassProperties.Add(classProperty);
            }

            return classObject.ClassProperties.Count > 0 ? new List<CSharpClassObject> { classObject } : new List<CSharpClassObject>();
        }

        private List<CSharpClassObject> ParseClassDeclaration(ClassDeclarationSyntax classDeclaration, string namespaceName)
        {
            var list = new List<CSharpClassObject>();

            List<string> baseName = new List<string>();
            string baseNamespace = null;

            if (classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Count > 0)
            {
                var baseTypeSyntax = classDeclaration.BaseList.Types.FirstOrDefault();
                if (baseTypeSyntax != null)
                {
                    var baseTypeName = baseTypeSyntax.Type.ToString();
                    baseName = baseTypeName.Split('.').ToList();
                    baseNamespace = string.Join('.', baseTypeName.Split('.').Reverse().Skip(1).Reverse());
                }
            }

            var classObject = new CSharpClassObject
            {
                Name = classDeclaration.Identifier.Text,
                FullyQualifiedName = $"{namespaceName}.{classDeclaration.Identifier.Text}",
                ClassNamespace = namespaceName,
                Summary = ExtractSummaryFromNode(classDeclaration),
                BaseClassNames = baseName,
                BaseClassNamespace = baseNamespace
            };


            // Step 2: Extract Class-Level DisplayName and Description Attributes
            foreach (var attributeListSyntax in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeListSyntax.Attributes)
                {
                    if (attribute.Name.ToString().Contains("Display"))
                    {
                        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                        if (argument != null)
                        {
                            classObject.DisplayName = argument.ToString().Replace("\"", "").Replace("Name = ", ""); // Assign the DisplayName
                        }
                    }

                    if (attribute.Name.ToString().Contains("DisplayName"))
                    {
                        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                        if (argument != null)
                        {
                            classObject.DisplayName = argument.ToString().Trim('"');
                        }
                    }

                    if (attribute.Name.ToString().Contains("Description"))
                    {
                        var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                        if (argument != null)
                        {
                            classObject.Description = argument.ToString().Trim('"');
                        }
                    }
                }
            }

            // Step 3: Process Class Members (Properties)
            foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                var classProperty = new ClassProperty
                {
                    PropertyName = member.Identifier.Text,
                    Type = ExtractPropertyType(member),
                    Summary = ExtractSummaryFromNode(member),
                    propertyDeclarationSyntax = member
                };

                   
                // Assign DisplayName and Description based on attributes
                foreach (var attributeListSyntax in member.AttributeLists)
                {
                    foreach (var attribute in attributeListSyntax.Attributes)
                    {
                        if (attribute.Name.ToString().Contains("DisplayName"))
                        {
                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                            if (argument != null)
                            {
                                classProperty.DisplayName = argument.ToString().Trim('"');
                            }
                        }

                        if (attribute.Name.ToString().Contains("Display"))
                        {
                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                            if (argument != null)
                            {
                                classProperty.DisplayName = argument.ToString().Replace("\"", "").Replace("Name = ", ""); // Assign the DisplayName
                            }
                        }


                        if (attribute.Name.ToString().Contains("Description"))
                        {
                            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                            if (argument != null)
                            {
                                classProperty.Description = argument.ToString().Trim('"'); // Assign the Description
                            }
                        }

                        // Handle Scaffolding attributes
                        if (attribute.Name.ToString().Contains("Scaffolding"))
                        {
                            classProperty.ScaffoldingAtt.Has = true;
                            HandleScaffoldingAttributes(attribute, classProperty);
                        }

                        // Handle UI Design attributes
                        if (attribute.Name.ToString().Contains("UIDesign"))
                        {
                            classProperty.UIDesignAtt.Has = true;
                            HandleUIDesignAttributes(attribute, classProperty);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(classProperty.DisplayName))
                {
                    classProperty.DisplayName = Utility.SplitCamelCase(member.Identifier.Text);
                }

                classObject.ClassProperties.Add(classProperty);
            }

            list.Add(classObject);
            return list;
        }

        private void HandleScaffoldingAttributes(AttributeSyntax attribute, ClassProperty classProperty)
        {
            foreach (var argument in attribute.ArgumentList.Arguments)
            {
                var argumentName = argument.Expression.ToString();

                if (argumentName.Contains("PropRole.Identifier"))
                {
                    classProperty.ScaffoldingAtt.PropRole = "Identifier";
                }
                else if (argumentName.Contains("PropRole.Searchable"))
                {
                    classProperty.ScaffoldingAtt.PropRole = "Searchable";
                }
                else if (argumentName.Contains("PropRole.Relationship"))
                {
                    classProperty.ScaffoldingAtt.PropRole = "Relationship";
                }

                if (argumentName.Contains("RelationshipType.OneToOne"))
                {
                    classProperty.ScaffoldingAtt.RelationshipType = "OneToOne";
                }
                else if (argumentName.Contains("RelationshipType.OneToMany"))
                {
                    classProperty.ScaffoldingAtt.RelationshipType = "OneToMany";
                }
                else if (argumentName.Contains("RelationshipType.ManyToOne"))
                {
                    classProperty.ScaffoldingAtt.RelationshipType = "ManyToOne";
                }
                else if (argumentName.Contains("RelationshipType.ManyToMany"))
                {
                    classProperty.ScaffoldingAtt.RelationshipType = "ManyToMany";
                }

                if (argumentName.Contains("DeleteBehavior.Cascade"))
                {
                    classProperty.ScaffoldingAtt.DeleteBehavior = "Cascade";
                }
                else if (argumentName.Contains("DeleteBehavior.Restrict"))
                {
                    classProperty.ScaffoldingAtt.DeleteBehavior = "Restrict";
                }
                else if (argumentName.Contains("DeleteBehavior.SetNull"))
                {
                    classProperty.ScaffoldingAtt.DeleteBehavior = "SetNull";
                }
                else if (argumentName.Contains("DeleteBehavior.NoAction"))
                {
                    classProperty.ScaffoldingAtt.DeleteBehavior = "NoAction";
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
                classProperty.ScaffoldingAtt.InverseProperty = _temp;
            }

            //Assign ForeignKeyProperty value
            var foreignKeyArgument = attribute.ArgumentList.Arguments
                .FirstOrDefault(arg => arg.ToString().Contains("foreignKeyProperty"));

            if (foreignKeyArgument != null)
            {
                _temp = foreignKeyArgument.ToString();
                _temp = _temp.Replace("foreignKeyProperty: \"", "");
                _temp = _temp.Replace("\"", "");
                classProperty.ScaffoldingAtt.ForeignKeyProperty = _temp;
            }

            //Assign LinkingTable value
            var linkingArgument = attribute.ArgumentList.Arguments
            .FirstOrDefault(arg => arg.ToString().Contains("linkingTable"));

            if (linkingArgument != null)
            {
                _temp = linkingArgument.ToString();
                _temp = _temp.Replace("linkingTable: \"", "");
                _temp = _temp.Replace("\"", "");
                classProperty.ScaffoldingAtt.LinkingTable = _temp;
            }
        }

        private void HandleUIDesignAttributes(AttributeSyntax attribute, ClassProperty classProperty)
        {
            foreach (var argument in attribute.ArgumentList.Arguments)
            {
                var argumentName = argument.NameColon?.ToString().Replace(":","");
                var argumentValue = argument.Expression.ToString();

                if (argumentName.Equals("componentType"))
                {
                    classProperty.UIDesignAtt.CompType = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("dataModel"))
                {
                    classProperty.UIDesignAtt.DataModel = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("helperText"))
                {
                    classProperty.UIDesignAtt.HelperText = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("helperTextOnFocus"))
                {
                    classProperty.UIDesignAtt.HelperTextOnFocus = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("disabled"))
                {
                    classProperty.UIDesignAtt.Disabled = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("readOnly"))
                {
                    classProperty.UIDesignAtt.ReadOnly = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("shrinkLabel"))
                {
                    classProperty.UIDesignAtt.ShrinkLabel = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("label"))
                {
                    classProperty.UIDesignAtt.Label = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("maxLength"))
                {
                    classProperty.UIDesignAtt.MaxLength = ExtractIntValue(argumentValue);
                }
                if (argumentName.Equals("counter"))
                {
                    classProperty.UIDesignAtt.Counter = ExtractIntValue(argumentValue);
                }
                if (argumentName.Equals("immediate"))
                {
                    classProperty.UIDesignAtt.Immediate = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("adornment"))
                {
                    classProperty.UIDesignAtt.Adornment = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("adornmentIcon"))
                {
                    classProperty.UIDesignAtt.AdornmentIcon = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("adornmentColor"))
                {
                    classProperty.UIDesignAtt.AdornmentColor = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("clearable"))
                {
                    classProperty.UIDesignAtt.Clearable = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("format"))
                {
                    classProperty.UIDesignAtt.Format = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("lines"))
                {
                    classProperty.UIDesignAtt.Lines = ExtractIntValue(argumentValue);
                }
                if (argumentName.Equals("autoGrow"))
                {
                    classProperty.UIDesignAtt.AutoGrow = ExtractBoolValue(argumentValue);
                }
                if (argumentName.Equals("maxLines"))
                {
                    classProperty.UIDesignAtt.MaxLines = ExtractIntValue(argumentValue);
                }
                if (argumentName.Equals("inputType"))
                {
                    classProperty.UIDesignAtt.InputType = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("mask"))
                {
                    classProperty.UIDesignAtt.Mask = ExtractStringValue(argumentValue);
                }
                if (argumentName.Equals("variant"))
                {
                    classProperty.UIDesignAtt.Variant = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("typography"))
                {
                    classProperty.UIDesignAtt.Typography = ExtractEnumValue(argumentValue);
                }
                if (argumentName.Equals("margin"))
                {
                    classProperty.UIDesignAtt.Margin = ExtractEnumValue(argumentValue);
                }
            }
        }

        
        private string? ExtractEnumValue(string argumentName)
        {
            string value = argumentName.Split('.').LastOrDefault()?.Replace("\"", "").Trim();
            if (value == "None") { return null; }
            return value;
        }

        private string? ExtractStringValue(string argumentName)
        {
            if(argumentName == "null")
            {
                return null;
            }
            string value  = argumentName.Replace("\"", "").Trim();
            return value;
        }

        private bool? ExtractBoolValue(string argumentName)
        {
            var boolValue = argumentName.Trim();
            bool? value = bool.TryParse(boolValue, out var result) ? result : (bool?)null;
            return value;
        }

        private int? ExtractIntValue(string argumentName)
        {

            var intValue = argumentName.Trim();
            int? value = int.TryParse(intValue, out var result) ? result : (int?)null;
            if(value == 0)
            {
                return null;
            }
            return value;
        }

        private string ExtractSummaryFromNode(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia().ToString();
            if (string.IsNullOrWhiteSpace(trivia))
            {
                return null;
            }

            var summaryMatch = Regex.Match(trivia, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
            return summaryMatch.Success ? summaryMatch.Groups[1].Value.Trim() : trivia.Trim();
        }

        private PropertyType ExtractPropertyType(PropertyDeclarationSyntax propertyDeclaration)
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


            // Create and populate the PropertyType instance
            var type = new PropertyType();
            type.TypeName = typeSyntax.ToString();
            type.IsArray = typeSyntax is ArrayTypeSyntax;
            type.IsList = isList;
            type.IsDictionary = isDictionary;
            type.IsNullable = isNullable;
            type.IsKnownType = isKnownType;
            type.IsICollection = isICollection;
            type.IsIEnumerable = isIEnumerable;
            type.IsKnownBaseType = isKnownBaseType;

            return type;
        }
    }
}
