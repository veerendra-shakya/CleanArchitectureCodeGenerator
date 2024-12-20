﻿using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Humanizer;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.UI.Pages
{
    public static class ListPage_razor
    {
        public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory, bool force = false)
        {
            if (!Helper.IsValidModel(modalClassObject)) return;
            FileInfo? targetFile = Helper.GetFileInfo(relativeTargetPath, targetProjectDirectory, force);
            if (targetFile == null)
            {
                return;
            }

            try
            {
                var relativePath = Utility.MakeRelativePath(ApplicationHelper.RootDirectory, Path.GetDirectoryName(targetFile.FullName) ?? "");
                string templateFilePath = Utility.GetTemplateFile(relativePath, targetFile.FullName);
                string templateContent = File.ReadAllText(templateFilePath, Encoding.UTF8);
                string NamespaceName = Helper.GetNamespace(relativePath);
                string mudTdHeaderDefinition = CreateMudTdHeaderDefinition(modalClassObject);
                string? masterProperty = modalClassObject.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();
                var (pageAttribute, parameterProperty, queryCondition, createCondition) = CreateForeignKeyCode(modalClassObject);

                // Initialize MasterData object
                var masterdata = new
                {
                    rootdirectory = ApplicationHelper.RootDirectory,
                    rootnamespace = ApplicationHelper.RootNamespace,
                    namespacename = NamespaceName,
                    domainprojectname = ApplicationHelper.DomainProjectName,
                    uiprojectname = ApplicationHelper.UiProjectName,
                    infrastructureprojectname = ApplicationHelper.InfrastructureProjectName,
                    applicationprojectname = ApplicationHelper.ApplicationProjectName,
                    domainprojectdirectory = ApplicationHelper.DomainProjectDirectory,
                    infrastructureprojectdirectory = ApplicationHelper.InfrastructureProjectDirectory,
                    uiprojectdirectory = ApplicationHelper.UiProjectDirectory,
                    applicationprojectdirectory = ApplicationHelper.ApplicationProjectDirectory,
                    modelnameplural = modalClassObject.NamePlural,
                    modelname = modalClassObject.Name,
                    mudtdheaderdefinition = mudTdHeaderDefinition,
                    masterproperty = masterProperty,
                    fkpageattribute = pageAttribute,
                    fkparameterproperty = parameterProperty, 
                    fkquerycondition = queryCondition,
                    createcondition = createCondition

                };

                // Parse and render the class template
                var classTemplate = Template.Parse(templateContent);
                string generatedClass = classTemplate.Render(masterdata);

                if (!string.IsNullOrEmpty(generatedClass))
                {
                    Utility.WriteToDiskAsync(targetFile.FullName, generatedClass);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Created file: {relativeTargetPath}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error generating file '{relativeTargetPath}': {ex.Message}");
                Console.ResetColor();
            }
        }

        private static string CreateMudTdHeaderDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();

            // Loop through other properties
            foreach (var property in classObject.ClassProperties)
            {
                if (property.PropertyName == "Id" || property.PropertyName.EndsWith("Id")) continue;
                if (property.PropertyName == "Deleted") continue;
                if (property.PropertyName == "DeletedOn") continue;
                if (property.PropertyName == "DeletedBy") continue;
                if (!property.DataUsesAtt.VisibleOnGrid && property.DataUsesAtt.PrimaryRole != "Relationship") continue;

                if (HasAttribute(property, "DataEditor"))
                {
                    output.Append(HandleDataEditor(property));
                    continue;
                }

                // Handle relationship properties
                if (property.DataUsesAtt.PrimaryRole == "Relationship")
                {
                    // Extract the related class name from the type (handle collections and direct references)
                    var relatedClassName = Utility.ExtractClassNameFromType(property.Type.TypeName);

                    // Get the related class from the cache
                    var relatedClass = ApplicationHelper.ClassObjectList.FirstOrDefault(c => c.Name == relatedClassName);
                    if (relatedClass != null)
                    {
                        if (property.Type.IsList || property.Type.IsICollection)
                        {
                            output.Append(GenerateTemplateColumnForOtherRelationships(property, relatedClass));
                        }
                        else
                        {
                            output.Append(GenerateTemplateColumnForOneToOne(property, relatedClass));

                        }
                    }
                    continue;
                }

                // Regular property
                output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");

            }
            return output.ToString();
        }


        private static string HandleDataEditor(ClassProperty property)
        {
            var output = new StringBuilder();

            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("DataEditor"));

            if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 1)
            {
                var arg = attribute.ArgumentList.Arguments[0].ToString();

                // Remove EditorType. prefix if present
                if (arg.StartsWith("EditorType."))
                {
                    arg = arg.Replace("EditorType.", string.Empty);
                }

                switch (arg)
                {
                    case "Upload":
                        output.Append(GenerateUploadColumn(property));
                        break;

                    case "ListStringTextEditor":
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName} != null ? x.{property.PropertyName}.Count : 0\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                    case "ListStringCheckBoxMultiSelection":
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName} != null ? x.{property.PropertyName}.Count : 0\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                    case "ListStringMudSelectMultiSelection":
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName} != null ? x.{property.PropertyName}.Count : 0\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                    case "HtmlEditor":
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName} != null ? x.{property.PropertyName}.Length : 0\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                    case "SlugTextField":
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                    default:
                        output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\" />");
                        break;
                }

            }
            return output.ToString();
        }

        private static string GenerateUploadColumn(ClassProperty property)
        {
            var output = new StringBuilder();
            if (property.Type.TypeName.Contains("List<JsonImage>?"))
            {
                output.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x => x.{property.PropertyName})]\">");
                output.AppendLine($"    <CellTemplate>");
                output.AppendLine($"        <MudBadge Content=\"@(context.Item.{property.PropertyName} != null ? context.Item.{property.PropertyName}.Count : 0)\" Overlap=\"true\" Class=\"mx-6 my-4\">");
                output.AppendLine($"            <MudIcon Icon=\"@(context.Item.{property.PropertyName} != null ? Icons.Material.Filled.Image : Icons.Material.Filled.BrokenImage)\" Color=\"@(context.Item.{property.PropertyName} != null ? Color.Success : Color.Error)\" />");
                output.AppendLine($"        </MudBadge>");
                output.AppendLine($"    </CellTemplate>");
                output.AppendLine($"</TemplateColumn>");
            }
            if (property.Type.TypeName.Contains("List<JsonFile>?"))
            {
                output.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x => x.{property.PropertyName})]\">");
                output.AppendLine($"    <CellTemplate>");
                output.AppendLine($"        <MudBadge Content=\"@(context.Item.{property.PropertyName} != null ? context.Item.{property.PropertyName}.Count : 0)\" Overlap=\"true\" Class=\"mx-6 my-4\">");
                output.AppendLine($"            <MudIcon Icon=\"@(context.Item.{property.PropertyName} != null ? Icons.Material.Filled.FilePresent : Icons.Material.Filled.BrokenImage)\" Color=\"@(context.Item.{property.PropertyName} != null ? Color.Success : Color.Error)\" />");
                output.AppendLine($"        </MudBadge>");
                output.AppendLine($"    </CellTemplate>");
                output.AppendLine($"</TemplateColumn>");
            }
            if (property.Type.TypeName.Contains("JsonImage?"))
            {
                output.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x => x.{property.PropertyName})]\">");
                output.AppendLine($"    <CellTemplate>");
                output.AppendLine($"        <MudIcon Icon=\"@(context.Item.{property.PropertyName}?.Url != null ? Icons.Material.Filled.Image : Icons.Material.Filled.BrokenImage)\" Color=\"@(context.Item.{property.PropertyName}?.Url != null ? Color.Success : Color.Error)\" />");
                output.AppendLine($"    </CellTemplate>");
                output.AppendLine($"</TemplateColumn>");
            }
            if (property.Type.TypeName.Contains("JsonFile?"))
            {
                output.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x => x.{property.PropertyName})]\">");
                output.AppendLine($"    <CellTemplate>");
                output.AppendLine($"        <MudIcon Icon=\"@(context.Item.{property.PropertyName}?.Url != null ? Icons.Material.Filled.FilePresent : Icons.Material.Filled.BrokenImage)\" Color=\"@(context.Item.{property.PropertyName}?.Url != null ? Color.Success : Color.Error)\" />");
                output.AppendLine($"    </CellTemplate>");
                output.AppendLine($"</TemplateColumn>");
            }
            return output.ToString();
        }


        private static (string PageAttribute, string ParameterProperty, string QueryCondition, string CreateCondition) CreateForeignKeyCode(CSharpClassObject model)
        {
            var foreignKeyProperty = model.ClassProperties.Where(p => p.DataUsesAtt.IsForeignKey).FirstOrDefault();

            if (foreignKeyProperty != null)
            {
                string pageAttribute = $"@page \"/pages/{model.NamePlural}/{{{foreignKeyProperty.PropertyName}:guid?}}\"";
                string parameterProperty = $"[Parameter] public Guid? {foreignKeyProperty.PropertyName} {{ get; set; }}";
                string queryCondition = $"Query.{foreignKeyProperty.PropertyName} = {foreignKeyProperty.PropertyName}.HasValue ? {foreignKeyProperty.PropertyName}.Value : Guid.Empty;";
                string createCondition = $"if ({foreignKeyProperty.PropertyName}.HasValue) {{ command.{foreignKeyProperty.PropertyName} = {foreignKeyProperty.PropertyName}.Value; }}";

                return (pageAttribute, parameterProperty, queryCondition, createCondition);
            }

            // Return empty strings if no foreign key property is found
            return (string.Empty, string.Empty, string.Empty,string.Empty);
        }


        #region Helper Functions
        private static bool HasAttribute(ClassProperty property, string attributeName)
        {
            var attributeLists = property.propertyDeclarationSyntax.AttributeLists;
            var attributes = attributeLists.SelectMany(a => a.Attributes);
            var attributeNames = attributes.Select(a => a.Name.ToString());
            var hasAttribute = attributeNames.Any(name => name.Contains(attributeName));
            return hasAttribute;
        }

        // Generates a TemplateColumn for OneToOne relationships using StringBuilder
        private static string GenerateTemplateColumnForOneToOne(ClassProperty property, CSharpClassObject relatedClass)
        {
            var masterProperty = relatedClass.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Identifier").FirstOrDefault();
            var displayProperties = relatedClass.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Searchable").ToList();
            if (masterProperty != null) { displayProperties.Add(masterProperty); }

            var sb = new StringBuilder();


            sb.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\">");
            sb.AppendLine("    <CellTemplate>");
            sb.AppendLine($"        @if(context.Item.{property.PropertyName} != null)");
            sb.AppendLine("        {");

            // Loop through each display property and append the span tag for each, avoiding trailing commas
            for (int i = 0; i < displayProperties.Count; i++)
            {
                var displayProperty = displayProperties[i];
                sb.AppendLine($"            <MudText>{displayProperty.DisplayName}: @context.Item.{property.PropertyName}.{displayProperty.PropertyName}</MudText>");

                //if (i < displayProperties.Count - 1)
                //{
                //    sb.AppendLine("<br/>"); // Add comma only if it's not the last element
                //}
            }

            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            <MudText>No {relatedClass.Name} Information</MudText>");
            sb.AppendLine("        }");
            sb.AppendLine("    </CellTemplate>");
            sb.AppendLine("</TemplateColumn>");

            return sb.ToString();
        }

        // Generates a TemplateColumn for relationships that have collections (OneToMany, ManyToMany) using StringBuilder
        private static string GenerateTemplateColumnForOtherRelationships(ClassProperty property, CSharpClassObject relatedClass)
        {
            var masterProperty = relatedClass.ClassProperties.FirstOrDefault(p => p.DataUsesAtt.PrimaryRole == "Identifier");
            var displayProperties = relatedClass.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Searchable").ToList();
            if (masterProperty != null)
            {
                displayProperties.Add(masterProperty);
            }

            var sb = new StringBuilder();

            sb.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDisplayName(x=>x.{property.PropertyName})]\">");
            sb.AppendLine("    <CellTemplate>");
            sb.AppendLine($"        @if (context.Item.{property.PropertyName} != null && context.Item.{property.PropertyName}.Count > 0)");
            sb.AppendLine("        {");

            sb.AppendLine("            <MudTooltip Arrow=\"true\" Placement=\"Placement.Left\">");
            sb.AppendLine("                <ChildContent>");
            sb.AppendLine($"                <a href=\"/pages/{property.Type.TypeName.Replace("ICollection<", "").Replace(">", "").Pluralize()}/@context.Item.Id\" target=\"blank\">");
            sb.AppendLine($"                    <MudChip Color=\"Color.Default\">@context.Item.{property.PropertyName}.Count</MudChip>");
            sb.AppendLine("                </a>");
            sb.AppendLine("                </ChildContent>");
            sb.AppendLine("                <TooltipContent>");
            sb.AppendLine($"                    @foreach (var item in context.Item.{property.PropertyName})");
            sb.AppendLine("                    {");
            if (masterProperty != null)
            {
                sb.AppendLine($"                        <MudText Align=\"Align.Left\" Typo=\"Typo.body2\">- @item.{masterProperty.PropertyName}</MudText>");
            }
            sb.AppendLine("                    }");
            sb.AppendLine("                </TooltipContent>");
            sb.AppendLine("            </MudTooltip>");

            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            <a href=\"/pages/{property.Type.TypeName.Replace("ICollection<", "").Replace(">", "").Pluralize()}/@context.Item.Id\" target=\"blank\">");
            sb.AppendLine($"                <MudChip Color=\"Color.Default\">0</MudChip>");
            sb.AppendLine("             </a>");
            sb.AppendLine("        }");
            sb.AppendLine("    </CellTemplate>");
            sb.AppendLine("</TemplateColumn>");

            return sb.ToString();
        }

        #endregion
    }
}
