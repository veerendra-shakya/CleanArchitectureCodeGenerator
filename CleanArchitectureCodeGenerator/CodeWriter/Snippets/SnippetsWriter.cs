using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public class SnippetsWriter
    {

        public const string PRIMARYKEY = "Id";

        public string CreateDtoFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();

            foreach (var property in classObject.ClassProperties)
            {
                output.AppendLine($"    [Description(\"{property.DisplayName}\")]");
                if (property.PropertyName == PRIMARYKEY)
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

        public string CreateImportFuncExpression(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == PRIMARYKEY) continue;

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

        public string CreateTemplateFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                if (property.PropertyName == PRIMARYKEY) continue;
                output.AppendLine($"_localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})],");
            }
            return output.ToString();
        }

        public string CreateExportFuncExpression(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"{{ _localizer[_dto.GetMemberDescription(x=>x.{property.PropertyName})], item => item.{property.PropertyName} }},");
            }
            return output.ToString();
        }

        public string CreateMudTdHeaderDefinition(CSharpClassObject classObject)
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
                if (property.PropertyName == PRIMARYKEY || property.PropertyName.EndsWith("Id")) continue;

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

        public string CreateMudTdDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            var defaultFieldNames = new string[] { "Name", "Description" };

            if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && defaultFieldNames.Contains(x.PropertyName)))
            {
                output.AppendLine("<MudTd HideSmall=\"false\" DataLabel=\"@L[_currentDto.GetMemberDescription(x=>x.Name)]\">");
                output.AppendLine("    <div class=\"d-flex flex-column\">");

                if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.First()))
                {
                    output.AppendLine("        <MudText>@context.Name</MudText>");
                }
                if (classObject.ClassProperties.Any(x => x.Type.IsKnownType && x.PropertyName == defaultFieldNames.Last()))
                {
                    output.AppendLine("        <MudText Typo=\"Typo.body2\" Class=\"mud-text-secondary\">@context.Description</MudText>");
                }

                output.AppendLine("    </div>");
                output.AppendLine("</MudTd>");
            }

            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType && !defaultFieldNames.Contains(x.PropertyName)))
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

      

        public string CreateFieldAssignmentDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType && x.PropertyName != PRIMARYKEY))
            {
                output.AppendLine($"        {property.PropertyName} = dto.{property.PropertyName},");
            }
            return output.ToString();
        }

        public string CreateAdvancedSpecificationQuery(CSharpClassObject classObject)
        {
            // Get the item name from classObject.Name
            var itemName = classObject.Name;

            // Get the master property for identifier role
            var masterProperty = classObject.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();
            // Get the list of searchable properties
            var searchableProperty = classObject.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").Select(p => p.PropertyName).ToList();
            if( masterProperty!=null)
            {
                searchableProperty.Add(masterProperty);
            }
            var output = new StringBuilder();

            // Start building the query with {itemname} replaced
            output.AppendLine($"Query.Where(q => q.{masterProperty} != null)");

            // Build the search condition for searchable properties
            if (searchableProperty.Any())
            {
                output.Append("         .Where(q => ");
                for (int i = 0; i < searchableProperty.Count; i++)
                {
                    output.Append($"q.{searchableProperty[i]}!.Contains(filter.Keyword)");

                    if (i < searchableProperty.Count - 1)
                    {
                        output.Append(" || ");
                    }
                }
                output.AppendLine(", !string.IsNullOrEmpty(filter.Keyword))");
            }

            // Add the rest of the conditions with {itemname} replaced
            output.AppendLine($"            .Where(q => q.CreatedBy == filter.CurrentUser.UserId, filter.ListView == {itemName}ListView.My && filter.CurrentUser is not null)");
            output.AppendLine($"            .Where(q => q.Created >= start && q.Created <= end, filter.ListView == {itemName}ListView.CreatedToday)");
            output.AppendLine($"            .Where(q => q.Created >= last30day, filter.ListView == {itemName}ListView.Created30Days);");

            // Return the built query as a string
            return output.ToString();
        }

        public string CreateSearchableProperties(CSharpClassObject model)
        {
            var masterProperty = model.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
            var searchableProperties = model.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

            if (masterProperty != null)
            {
                // Insert masterProperty at the beginning of the list
                searchableProperties.Insert(0, masterProperty);
            }

            var sb = new StringBuilder();

            foreach (var property in searchableProperties)
            {
                sb.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
            }

            return sb.ToString();
        }

        // Generates a TemplateColumn for OneToOne relationships using StringBuilder
        private string GenerateTemplateColumnForOneToOne(ClassProperty property, CSharpClassObject relatedClass)
        {
            var masterProperty = relatedClass.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").FirstOrDefault();
            var displayProperties = relatedClass.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();
            if (masterProperty != null){ displayProperties.Add(masterProperty);}

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
        private string GenerateTemplateColumnForOtherRelationships(ClassProperty property, CSharpClassObject relatedClass)
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
            sb.AppendLine("            <MudChipSet>");

            // Loop through display properties to show them inside chips for each item in the collection
            sb.AppendLine($"            @foreach (var item in context.Item.{property.PropertyName})");
            sb.AppendLine("            {");

            // For each display property, generate a MudChip
            foreach (var displayProperty in displayProperties)
            {
                sb.AppendLine($"                <MudChip Color=\"Color.Primary\">{displayProperty.DisplayName}: @item.{displayProperty.PropertyName}</MudChip>");
            }

            sb.AppendLine("            }");
            sb.AppendLine("            </MudChipSet>");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            <MudText>No {relatedClass.Name}</MudText>");
            sb.AppendLine("        }");
            sb.AppendLine("    </CellTemplate>");
            sb.AppendLine("</TemplateColumn>");

            return sb.ToString();
        }


    }
}

