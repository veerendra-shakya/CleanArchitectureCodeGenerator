using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.DTOs
{
    public static class Dto
    {
        public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory)
        {
            FileInfo? targetFile = Helper.GetFileInfo(relativeTargetPath, targetProjectDirectory);
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
                var dtoFieldDefinition = CreateDtoFieldDefinition(modalClassObject);

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
                    modelnameplural = modalClassObject.Name.Pluralize(),
                    modelname = modalClassObject.Name,
                    dtofielddefinition = dtoFieldDefinition,
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

        private static string CreateDtoFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();

            foreach (var property in classObject.ClassProperties)
            {
                output.AppendLine($"    [Description(\"{property.DisplayName}\")]");
                if (property.PropertyName == "Id")
                {
                    output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
                }
                else if (property.Type.IsList || property.Type.IsICollection)
                {
                    string _proptype = property.Type.TypeName;
                    _proptype = _proptype.Replace("IList", "List");
                    _proptype = _proptype.Replace("ICollection", "List");
                    output.AppendLine($"    public {_proptype} {property.PropertyName} {{get;set;}} = new();");
                }
                else
                {
                    switch (property.Type.TypeName)
                    {
                        case "string" when property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase):
                            output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}} = string.Empty;");
                            break;
                        case "string" when !property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase) && !property.Type.IsArray && !property.Type.IsDictionary:
                            output.AppendLine($"    public {property.Type.TypeName}? {property.PropertyName} {{get;set;}}");
                            break;
                        case "string" when !property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase) && property.Type.IsArray:
                            output.AppendLine($"    public HashSet<{property.Type.TypeName}>? {property.PropertyName} {{get;set;}}");
                            break;
                        case "string":
                            output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}} = string.Empty;");
                            break;
                        case "System.DateTime?":
                        case "System.DateTime":
                        case "decimal?":
                        case "decimal":
                        case "int?":
                        case "int":
                        case "double?":
                        case "double":
                            output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
                            break;
                        default:
                            output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
                            break;
                    }

                }
            }
            return output.ToString();
        }

    }
}
