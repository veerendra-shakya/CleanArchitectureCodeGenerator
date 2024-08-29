using CleanArchitecture.CodeGenerator.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CleanArchitecture.CodeGenerator.Helpers;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CleanArchitecture.CodeGenerator
{
    public static class TemplateMapper
    {
        private static readonly List<string> _templateFiles = new List<string>();
        private const string _defaultExt = ".txt";
    
        public const string PRIMARYKEY = "Id";

        static TemplateMapper()
        {
            var assembly = Assembly.GetExecutingAssembly().Location;
            var _template_folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
            _templateFiles.AddRange(Directory.GetFiles(_template_folder, "*" + _defaultExt, SearchOption.AllDirectories));
        }

        public static async Task<string> GenerateClass(IntellisenseObject ModalClassObject, string FileFullName, string ModalClassName, string TargetProjectDirectory)
        {
            var relativePath = Utility.MakeRelativePath(CodeGenerator.ROOT_DIRECTORY, Path.GetDirectoryName(FileFullName) ?? "");
           
            string templateFile = GetTemplateFile(relativePath, FileFullName);

            var template = await ReplaceTokensAsync(ModalClassObject, ModalClassName, relativePath, templateFile, TargetProjectDirectory);
            return Utility.NormalizeLineEndings(template);
        }

        private static string GetTemplateFile(string relative, string file)
        {
            var list = _templateFiles.ToList();
            var templateFolders = new[]
            {
                "Commands\\AcceptChanges",
                "Commands\\Create",
                "Commands\\Delete",
                "Commands\\Update",
                "Commands\\AddEdit",
                "Commands\\Import",
                "DTOs",
                "Caching",
                "EventHandlers",
                "Events",
                "Specification",
                "Queries\\Export",
                "Queries\\GetAll",
                "Queries\\GetById",
                "Queries\\Pagination",
                "Pages",
                "Pages\\Components",
                "Persistence\\Configurations",
                "PermissionSet",
            };

            var extension = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);
            var safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);

            // Determine the folder pattern based on the relative path
            var folderPattern = templateFolders
                .FirstOrDefault(x => relative.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)
                ?.Replace("\\", "\\\\");

            if (!string.IsNullOrEmpty(folderPattern))
            {
                // Look for direct file name matches in the specified template folder
                var matchingFile = list
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault(f => Regex.IsMatch(f, folderPattern, RegexOptions.IgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(f).Split('.')
                                         .All(x => name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0));

                if (!string.IsNullOrEmpty(matchingFile))
                {
                    return matchingFile;
                }
            }

            // If no direct match, look for file extension matches
            var extensionMatch = list
                .FirstOrDefault(f => Path.GetFileName(f).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase) &&
                                     File.Exists(f));

            if (extensionMatch != null)
            {
                var adjustedName = AdjustForSpecific(safeName, extension);
                return Path.Combine(Path.GetDirectoryName(extensionMatch), adjustedName + _defaultExt);
            }

            // If no match is found, return null or throw an exception as per your requirement
            return null;
        }

        private static string AdjustForSpecific(string safeName, string extension)
        {
            if (Regex.IsMatch(safeName, "^I[A-Z].*"))
            {
                return extension += "-interface";
            }

            return extension;
        }
        private static async Task<string> ReplaceTokensAsync(IntellisenseObject ModalClassObject, string ModalClassName, string relativePath, string templateFile, string TargetProjectDirectory)
        {

            //using CleanArchitecture.Blazor.Application.Features.Customers.DTOs;
            if (string.IsNullOrEmpty(templateFile))
            {
                return templateFile;
            }

            var ns = CodeGenerator.ROOT_NAMESPACE;
            if (!string.IsNullOrEmpty(relativePath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(relativePath);
            }
            ns = ns.TrimEnd('.');

            using (var reader = new StreamReader(templateFile))
            {
                var content = await reader.ReadToEndAsync();
                var nameofPlural = Utility.Pluralize(ModalClassName);
                var dtoFieldDefinition = CreateDtoFieldDefinition(ModalClassObject);
                var importFuncExpression = CreateImportFuncExpression(ModalClassObject);
                var templateFieldDefinition = CreateTemplateFieldDefinition(ModalClassObject);
                var exportFuncExpression = CreateExportFuncExpression(ModalClassObject);
                var mudTdDefinition = CreateMudTdDefinition(ModalClassObject);
                var mudTdHeaderDefinition = CreateMudTdHeaderDefinition(ModalClassObject);
                var mudFormFieldDefinition = CreateMudFormFieldDefinition(ModalClassObject);
                var fieldAssignmentDefinition = CreateFieldAssignmentDefinition(ModalClassObject);

                return content.Replace("{rootnamespace}", CodeGenerator.ROOT_NAMESPACE)
                              .Replace("{selectns}", $"{CodeGenerator.ROOT_NAMESPACE}.{Utility.GetProjectNameFromPath(TargetProjectDirectory)}.Features")
                              .Replace("{namespace}", ns)
                              .Replace("{itemname}", ModalClassName)
                              .Replace("{nameofPlural}", nameofPlural)
                              .Replace("{dtoFieldDefinition}", dtoFieldDefinition)
                              .Replace("{fieldAssignmentDefinition}", fieldAssignmentDefinition)
                              .Replace("{importFuncExpression}", importFuncExpression)
                              .Replace("{templateFieldDefinition}", templateFieldDefinition)
                              .Replace("{exportFuncExpression}", exportFuncExpression)
                              .Replace("{mudTdDefinition}", mudTdDefinition)
                              .Replace("{mudTdHeaderDefinition}", mudTdHeaderDefinition)
                              .Replace("{mudFormFieldDefinition}", mudFormFieldDefinition);
            }
        }

        private static string CreateDtoFieldDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"    [Description(\"{Utility.SplitCamelCase(property.Name)}\")]");
                if (property.Name == PRIMARYKEY)
                {
                    output.AppendLine($"    public {property.Type.CodeName} {property.Name} {{get;set;}}");
                }
                else
                {
                    switch (property.Type.CodeName)
                    {
                        case "string" when property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase):
                            output.AppendLine($"    public {property.Type.CodeName} {property.Name} {{get;set;}} = string.Empty;");
                            break;
                        case "string" when !property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) && !property.Type.IsArray && !property.Type.IsDictionary:
                            output.AppendLine($"    public {property.Type.CodeName}? {property.Name} {{get;set;}}");
                            break;
                        case "string" when !property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) && property.Type.IsArray:
                            output.AppendLine($"    public HashSet<{property.Type.CodeName}>? {property.Name} {{get;set;}}");
                            break;
                        case "System.DateTime?":
                        case "System.DateTime":
                        case "decimal?":
                        case "decimal":
                        case "int?":
                        case "int":
                        case "double?":
                        case "double":
                            output.AppendLine($"    public {property.Type.CodeName} {property.Name} {{get;set;}}");
                            break;
                        default:
                            output.AppendLine($"    public {property.Type.CodeName} {property.Name} {{get;set;}}");
                            break;
                    }
                }
            }
            return output.ToString();
        }

        private static string CreateImportFuncExpression(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.Name == PRIMARYKEY) continue;
                if (property.Type.CodeName.StartsWith("bool"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.Name})], (row, item) => item.{property.Name} = Convert.ToBoolean(row[_localizer[_dto.GetMemberDescription(x=>x.{property.Name})]]) }},");
                }
                else
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.Name})], (row, item) => item.{property.Name} = row[_localizer[_dto.GetMemberDescription(x=>x.{property.Name})]].ToString() }},");
                }
            }
            return output.ToString();
        }

        private static string CreateTemplateFieldDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.Name == PRIMARYKEY) continue;
                output.AppendLine($"_localizer[_dto.GetMemberDescription(x=>x.{property.Name})],");
            }
            return output.ToString();
        }

        private static string CreateExportFuncExpression(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.Name})], item => item.{property.Name} }},");
            }
            return output.ToString();
        }

        private static string CreateMudTdHeaderDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            var defaultFieldNames = new string[] { "Name", "Description" };

            if (classObject.Properties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.Name)))
            {
                output.AppendLine("<PropertyColumn Property=\"x => x.Name\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("   <CellTemplate>");
                output.AppendLine("      <div class=\"d-flex flex-column\">");

                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.Name == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\">@context.Item.Name</MudText>");
                }
                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.Name == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Item.Description</MudText>");
                }
                output.AppendLine("     </div>");
                output.AppendLine("    </CellTemplate>");
                output.AppendLine("</PropertyColumn>");
            }

            foreach (var property in classObject.Properties.Where(x => !defaultFieldNames.Contains(x.Name)))
            {
                if (property.Name == PRIMARYKEY) continue;
                output.AppendLine($"<PropertyColumn Property=\"x => x.{property.Name}\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.Name})]\" />");
            }
            return output.ToString();
        }

        private static string CreateMudTdDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            var defaultFieldNames = new string[] { "Name", "Description" };

            if (classObject.Properties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.Name)))
            {
                output.AppendLine("<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("    <div class=\"d-flex flex-column\">");

                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.Name == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText>@context.Name</MudText>");
                }
                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.Name == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Description</MudText>");
                }

                output.AppendLine("    </div>");
                output.AppendLine("</MudTd>");
            }

            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType && !defaultFieldNames.Contains(x.Name)))
            {
                if (property.Name == PRIMARYKEY) continue;

                if (property.Type.CodeName.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.Name})]\" ><MudCheckBox Checked=\"@context.{property.Name}\" ReadOnly></MudCheckBox></MudTd>");
                }
                else if (property.Type.CodeName.Equals("System.DateTime", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.Name})]\" >@context.{property.Name}.Date.ToString(\"d\")</MudTd>");
                }
                else if (property.Type.CodeName.Equals("System.DateTime?", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.Name})]\" >@context.{property.Name}?.Date.ToString(\"d\")</MudTd>");
                }
                else
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.Name})]\" >@context.{property.Name}</MudTd>");
                }
            }
            return output.ToString();
        }

        private static string CreateMudFormFieldDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.Name == PRIMARYKEY) continue;
                switch (property.Type.CodeName.ToLower())
                {
                    case "string" when property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase):
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Value=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Required=\"true\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "string" when property.Name.Equals("Description", StringComparison.OrdinalIgnoreCase):
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" Lines=\"3\" For=\"@(() => model.{property.Name})\" @bind-Value=\"model.{property.Name}\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "bool?":
                    case "bool":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudCheckBox Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Checked=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\"></MudCheckBox>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "int?":
                    case "int":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Value=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Min=\"0\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "decimal?":
                    case "decimal":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Value=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Min=\"0.00m\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "double?":
                    case "double":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Value=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Min=\"0.00\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "system.datetime?":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudDatePicker Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Date=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudDatePicker>");
                        output.AppendLine("</MudItem>");
                        break;
                    default:
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.Name})]\" @bind-Value=\"model.{property.Name}\" For=\"@(() => model.{property.Name})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.Name).ToLower()} is required!\"]\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                }
            }
            return output.ToString();
        }

        private static string CreateFieldAssignmentDefinition(IntellisenseObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType && x.Name != PRIMARYKEY))
            {
                output.AppendLine($"        {property.Name} = dto.{property.Name},");
            }
            return output.ToString();
        }

    }
}