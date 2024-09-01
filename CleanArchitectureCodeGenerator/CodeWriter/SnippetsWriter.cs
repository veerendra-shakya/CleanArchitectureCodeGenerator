using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    public class SnippetsWriter
    {
        public const string PRIMARYKEY = "Id";

        public string CreateDtoFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            //List<ClassProperty> temp = classObject.Properties.ToList();
            // foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"    [Description(\"{Utility.SplitCamelCase(property.PropertyName)}\")]");
                if (property.PropertyName == PRIMARYKEY)
                {
                    output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
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

        public string CreateImportFuncExpression(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == PRIMARYKEY) continue;
                if (property.Type.TypeName.StartsWith("bool"))
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = Convert.ToBoolean(row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]]) }},");
                }
                else
                {
                    output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], (row, item) => item.{property.PropertyName} = row[_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})]].ToString() }},");
                }
            }
            return output.ToString();
        }

        public string CreateTemplateFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == PRIMARYKEY) continue;
                output.AppendLine($"_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})],");
            }
            return output.ToString();
        }

        public string CreateExportFuncExpression(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], item => item.{property.PropertyName} }},");
            }
            return output.ToString();
        }

        public string CreateMudTdHeaderDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            var defaultFieldNames = new string[] { "Name", "Description" };

            if (classObject.Properties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.PropertyName)))
            {
                output.AppendLine("<PropertyColumn Property=\"x => x.Name\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("   <CellTemplate>");
                output.AppendLine("      <div class=\"d-flex flex-column\">");

                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\">@context.Item.Name</MudText>");
                }
                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Item.Description</MudText>");
                }
                output.AppendLine("     </div>");
                output.AppendLine("    </CellTemplate>");
                output.AppendLine("</PropertyColumn>");
            }

            foreach (var property in classObject.Properties.Where(x => !defaultFieldNames.Contains(x.PropertyName)))
            {
                if (property.PropertyName == PRIMARYKEY) continue;
                output.AppendLine($"<PropertyColumn Property=\"x => x.{property.PropertyName}\" Title=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" />");
            }
            return output.ToString();
        }

        public string CreateMudTdDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            var defaultFieldNames = new string[] { "Name", "Description" };

            if (classObject.Properties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.PropertyName)))
            {
                output.AppendLine("<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("    <div class=\"d-flex flex-column\">");

                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText>@context.Name</MudText>");
                }
                if (classObject.Properties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Description</MudText>");
                }

                output.AppendLine("    </div>");
                output.AppendLine("</MudTd>");
            }

            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType && !defaultFieldNames.Contains(x.PropertyName)))
            {
                if (property.PropertyName == PRIMARYKEY) continue;

                if (property.Type.TypeName.StartsWith("bool", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" ><MudCheckBox Checked=\"@context.{property.PropertyName}\" ReadOnly></MudCheckBox></MudTd>");
                }
                else if (property.Type.TypeName.Equals("System.DateTime", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" >@context.{property.PropertyName}.Date.ToString(\"d\")</MudTd>");
                }
                else if (property.Type.TypeName.Equals("System.DateTime?", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" >@context.{property.PropertyName}?.Date.ToString(\"d\")</MudTd>");
                }
                else
                {
                    output.AppendLine($"<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.{property.PropertyName})]\" >@context.{property.PropertyName}</MudTd>");
                }
            }
            return output.ToString();
        }

        public string CreateMudFormFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == PRIMARYKEY) continue;
                switch (property.Type.TypeName.ToLower())
                {
                    case "string" when property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase):
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"true\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "string" when property.PropertyName.Equals("Description", StringComparison.OrdinalIgnoreCase):
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Lines=\"3\" For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "bool?":
                    case "bool":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudCheckBox Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Checked=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"></MudCheckBox>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "int?":
                    case "int":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "decimal?":
                    case "decimal":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0.00m\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "double?":
                    case "double":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0.00\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                        output.AppendLine("</MudItem>");
                        break;
                    case "system.datetime?":
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudDatePicker Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Date=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudDatePicker>");
                        output.AppendLine("</MudItem>");
                        break;
                    default:
                        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                        output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                        output.AppendLine("</MudItem>");
                        break;
                }
            }
            return output.ToString();
        }

        public string CreateFieldAssignmentDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.Properties.Where(x => x.Type.IsKnownType && x.PropertyName != PRIMARYKEY))
            {
                output.AppendLine($"        {property.PropertyName} = dto.{property.PropertyName},");
            }
            return output.ToString();
        }

    }
}
