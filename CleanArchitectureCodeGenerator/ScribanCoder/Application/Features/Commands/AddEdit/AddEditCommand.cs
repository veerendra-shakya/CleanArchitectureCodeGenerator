using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Humanizer;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Commands.AddEdit
{
    public static class AddEditCommand
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
                string CommandFieldDefinition = Helper.CreateCommandFieldDefinition(modalClassObject);
                string mapignore = GetMapIgnore(modalClassObject);
                string updatemanytomany = GetUpdateManyToManyCode2(modalClassObject);
                string addmanytomany = GetAddManyToManyCode(modalClassObject);

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
                    commandfielddefinition = CommandFieldDefinition,
                    mapignore,
                    updatemanytomany,
                    addmanytomany,
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

        private static string GetUpdateManyToManyCode(CSharpClassObject modalClassObject)
        {
            var output = new StringBuilder();

            foreach (var property in modalClassObject.ClassProperties)
            {
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
                    {
                        string LinkingTableName = property.ScaffoldingAtt.LinkingTable;
                        string key1 = $"{property.PropertyNameSingular}Id";
                        string key2 = $"{property.ScaffoldingAtt.InverseProperty.Singularize()}Id";
                        string PropertyType = property.Type.TypeName;
                        string DataType = Helper.ExtractDataType(PropertyType);
                        output.AppendLine();
                        output.AppendLine($"var related{property.PropertyName} = await _context.{DataType.Pluralize()}.Where(x => x.Id == request.Id).ToListAsync();");
                        output.AppendLine($"foreach (var {property.PropertyNameSingular.ToLower()} in request.{property.PropertyName})");
                        output.AppendLine($"{{");
                        output.AppendLine($"    if (!related{property.PropertyName}.Any(x => x.Id == {property.PropertyNameSingular.ToLower()}.Id))");
                        output.AppendLine($"    {{");
                        output.AppendLine($"        var additem = new {LinkingTableName}() {{{key2} = item.Id,{key1} = {property.PropertyNameSingular.ToLower()}.Id}};");
                        output.AppendLine($"        _context.{LinkingTableName.Pluralize()}.Add(additem);");
                        output.AppendLine($"    }}");
                        output.AppendLine($"}}");
                        output.AppendLine();
                    }
                }
            }
            return output.ToString();
        }

        private static string GetUpdateManyToManyCode2(CSharpClassObject modalClassObject)
        {
            var output = new StringBuilder();

            foreach (var property in modalClassObject.ClassProperties)
            {
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
                    {
                        string linkingTableName = property.ScaffoldingAtt.LinkingTable;
                        string key1 = $"{property.PropertyNameSingular}Id";
                        string key2 = $"{property.ScaffoldingAtt.InverseProperty.Singularize()}Id";
                        string propertyType = property.Type.TypeName;
                        string dataType = Helper.ExtractDataType(propertyType);

                        output.AppendLine($"#warning if auto-inclusion is Disabled on this side of many-to-many relationship. ADD \".Include(x => x.{property.PropertyName})\" in item query above'.\r\n");
                        output.AppendLine($"// Updating the related {property.PropertyName} (many-to-many relationship)");
                        output.AppendLine($"var current{property.PropertyName}Ids = item.{property.PropertyName}.Select(x => x.Id).ToList();");
                        output.AppendLine($"var updated{property.PropertyName}Ids = request.{property.PropertyName}.Select(x => x.Id).ToList();");

                        // Find items to add
                        output.AppendLine();
                        output.AppendLine($"// Find {property.PropertyName} to add");
                        output.AppendLine($"var {property.PropertyName.ToLower()}ToAdd = updated{property.PropertyName}Ids.Except(current{property.PropertyName}Ids).ToList();");
                        output.AppendLine($"foreach (var {property.PropertyNameSingular.ToLower()}Id in {property.PropertyName.ToLower()}ToAdd)");
                        output.AppendLine($"{{");
                        output.AppendLine($"    var addItem = new {linkingTableName}() {{ {key2} = item.Id, {key1} = {property.PropertyNameSingular.ToLower()}Id }};");
                        output.AppendLine($"    _context.{linkingTableName.Pluralize()}.Add(addItem);");
                        output.AppendLine($"}}");

                        // Find items to remove
                        output.AppendLine();
                        output.AppendLine($"// Find {property.PropertyName} to remove");
                        output.AppendLine($"var {property.PropertyName.ToLower()}ToRemove = current{property.PropertyName}Ids.Except(updated{property.PropertyName}Ids).ToList();");
                        output.AppendLine($"foreach (var {property.PropertyNameSingular.ToLower()}Id in {property.PropertyName.ToLower()}ToRemove)");
                        output.AppendLine($"{{");
                        output.AppendLine($"    var removeItem = await _context.{linkingTableName.Pluralize()}")
                              .AppendLine($"        .FirstOrDefaultAsync(x => x.{key2} == item.Id && x.{key1} == {property.PropertyNameSingular.ToLower()}Id, cancellationToken);");
                        output.AppendLine($"    if (removeItem != null)");
                        output.AppendLine($"    {{");
                        output.AppendLine($"        _context.{linkingTableName.Pluralize()}.Remove(removeItem);");
                        output.AppendLine($"    }}");
                        output.AppendLine($"}}");
                        output.AppendLine();
                    }
                }
            }
            return output.ToString();
        }
        
        private static string GetAddManyToManyCode(CSharpClassObject modalClassObject)
        {
            var output = new StringBuilder();

            foreach (var property in modalClassObject.ClassProperties)
            {
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
                    {
                        string linkingTableName = property.ScaffoldingAtt.LinkingTable;
                        string key1 = $"{property.PropertyNameSingular}Id";
                        string key2 = $"{property.ScaffoldingAtt.InverseProperty.Singularize()}Id";

                        // Add new items
                        output.AppendLine();
                        output.AppendLine($"// Add new {property.PropertyName.ToLower()} if any");
                        output.AppendLine($"foreach (var {property.PropertyNameSingular.ToLower()} in request.{property.PropertyName})");
                        output.AppendLine($"{{");
                        output.AppendLine($"    var addItem = new {linkingTableName}() {{ {key2} = item.Id, {key1} = {property.PropertyNameSingular.ToLower()}.Id }};");
                        output.AppendLine($"    _context.{linkingTableName.Pluralize()}.Add(addItem);");
                        output.AppendLine($"}}");
                        output.AppendLine();
                    }
                }
            }

            return output.ToString();
        }

        private static string GetMapIgnore(CSharpClassObject classObject)
        {
            var Unknown = classObject.ClassProperties
                .Where(x => x.Type.IsKnownType == false
                  && !x.Type.TypeName.Contains("JsonImage") 
                  && !x.Type.TypeName.Contains("JsonFile"))
                .ToList();
            var output = new StringBuilder();
            foreach (var property in Unknown)
            {
                if (property.Type.TypeName.Contains("Enum"))
                {
                    continue;
                }
                if (property.Type.IsList)
                {
                    continue;
                }
                if (property.Type.IsDictionary)
                {
                    continue;
                }

                output.Append($".ForMember(x => x.{property.PropertyName}, y => y.Ignore())");
            }
            return output.ToString();
        }
    }
}
