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


        public string CreateCommandFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();

            foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
            {
                output.AppendLine($"    [Description(\"{property.DisplayName}\")]");
                switch (property.Type.TypeName)
                {
                    case "string":
                        output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}} = string.Empty;");
                        break;
                    default:
                        output.AppendLine($"    public {property.Type.TypeName} {property.PropertyName} {{get;set;}}");
                        break;
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


   

        // Generates a TemplateColumn for OneToOne relationships using StringBuilder
        private string GenerateTemplateColumnForOneToOne(ClassProperty property, CSharpClassObject relatedClass)
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

