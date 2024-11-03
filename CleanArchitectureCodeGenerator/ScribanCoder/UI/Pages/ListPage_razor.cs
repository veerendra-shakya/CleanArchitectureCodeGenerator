using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
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
                string? masterProperty = modalClassObject.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();

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
            var defaultFieldNames = new string[] { "Name", "Description" };

            // Handling the default "Name" and "Description" properties
            if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.PropertyName)))
            {
                output.AppendLine("<PropertyColumn Property=\"x => x.Name\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("   <CellTemplate>");
                output.AppendLine("      <div class=\"d-flex flex-column\">");

                if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\">@context.Item.Name</MudText>");
                }
                if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Item.Description</MudText>");
                }
                output.AppendLine("     </div>");
                output.AppendLine("    </CellTemplate>");
                output.AppendLine("</PropertyColumn>");
            }

            // Loop through other properties
            foreach (var property in classObject.ClassProperties.Where(x => !defaultFieldNames.Contains(x.PropertyName)))
            {
                if (property.PropertyName == "Id" || property.PropertyName.EndsWith("Id")) continue;

                // Handle relationship properties
                if (property.ScaffoldingAtt.PropRole == "Relationship")
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
                }
                else
                {
                    // Regular property
                    output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" />");
                }

                // output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" />");
            }
            return output.ToString();
        }

        // Generates a TemplateColumn for OneToOne relationships using StringBuilder
        private static string GenerateTemplateColumnForOneToOne(ClassProperty property, CSharpClassObject relatedClass)
        {
            var masterProperty = relatedClass.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").FirstOrDefault();
            var displayProperties = relatedClass.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();
            if (masterProperty != null) { displayProperties.Add(masterProperty); }

            var sb = new StringBuilder();


            sb.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\">");
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
            var masterProperty = relatedClass.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
            var displayProperties = relatedClass.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();
            if (masterProperty != null)
            {
                displayProperties.Add(masterProperty);
            }

            var sb = new StringBuilder();

            sb.AppendLine($"<TemplateColumn Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\">");
            sb.AppendLine("    <CellTemplate>");
            sb.AppendLine($"        @if (context.Item.{property.PropertyName} != null && context.Item.{property.PropertyName}.Count > 0)");
            sb.AppendLine("        {");

            //sb.AppendLine("            <MudChipSet>");
            //sb.AppendLine($"            @foreach (var item in context.Item.{property.PropertyName})");
            //sb.AppendLine("            {");
            //foreach (var displayProperty in displayProperties)
            //{
            //    sb.AppendLine($"                <MudChip Color=\"Color.Primary\">{displayProperty.DisplayName}: @item.{displayProperty.PropertyName}</MudChip>");
            //}
            //sb.AppendLine("            }");
            //sb.AppendLine("            </MudChipSet>");

            sb.AppendLine("            <MudTooltip Arrow=\"true\" Placement=\"Placement.Left\">");
            sb.AppendLine("                <ChildContent>");
            sb.AppendLine($"                    <MudChip Color=\"Color.Default\">@context.Item.{property.PropertyName}.Count</MudChip>");
            sb.AppendLine("                </ChildContent>");
            sb.AppendLine("                <TooltipContent>");
            sb.AppendLine($"                    @foreach (var item in context.Item.{property.PropertyName})");
            sb.AppendLine("                    {");
            if (masterProperty!= null)
            {
            sb.AppendLine($"                        <MudText Align=\"Align.Left\" Typo=\"Typo.body2\">- @item.{masterProperty.PropertyName}</MudText>");
            }
            sb.AppendLine("                    }");
            sb.AppendLine("                </TooltipContent>");
            sb.AppendLine("            </MudTooltip>");

            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            <MudChip Color=\"Color.Default\">0</MudChip>");
            sb.AppendLine("        }");
            sb.AppendLine("    </CellTemplate>");
            sb.AppendLine("</TemplateColumn>");

            return sb.ToString();
        }




    }
}
