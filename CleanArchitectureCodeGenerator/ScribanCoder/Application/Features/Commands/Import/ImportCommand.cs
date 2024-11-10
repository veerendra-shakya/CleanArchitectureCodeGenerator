using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Commands.Import
{
    public static class ImportCommand
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

                string importFuncExpression = CreateImportFuncExpression(modalClassObject);
                string templateFieldDefinition = CreateTemplateFieldDefinition(modalClassObject);
                var masterProperty = modalClassObject.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();

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
                    importfuncexpression = importFuncExpression,
                    templatefielddefinition = templateFieldDefinition,
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

        private static string CreateImportFuncExpression(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == "Id") continue;

                var typeName = property.Type.TypeName;

                if (typeName.StartsWith("bool"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToBoolean(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("int"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToInt32(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("long"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToInt64(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("short"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToInt16(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("byte"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToByte(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("float"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToSingle(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("double"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToDouble(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("decimal"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToDecimal(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("DateTime"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToDateTime(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("Guid"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Guid.Parse(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]].ToString()) }},");
                }
                else if (typeName.StartsWith("char"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToChar(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else if (typeName.StartsWith("string"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]].ToString() }},");
                }
                else
                {
                    // Default case, handle as string if no specific type is matched
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]].ToString() }},");
                }
            }
            return output.ToString();
        }

        private static string CreateTemplateFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == "Id") continue;
                output.AppendLine($"_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})],");
            }
            return output.ToString();
        }
    }
}
