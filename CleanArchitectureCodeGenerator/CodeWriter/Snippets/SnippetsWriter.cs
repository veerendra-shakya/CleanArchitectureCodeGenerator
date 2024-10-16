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

    }
}

