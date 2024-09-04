using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class EfConfigurationsGenerator
    {
        public static string GenerateConfigurations(CSharpClassObject classObject)
        {
            var sb = new StringBuilder();

            foreach (var property in classObject.Properties)
            {
                var propertyName = property.PropertyName;
                var propertyType = property.Type;

                // Start the builder for the property configuration
                sb.Append($"builder.Property(x => x.{propertyName})");

                // Check for data type (HasColumnType)
                if (HasAttribute(property, "HasColumnType", out string columnType))
                {
                    sb.Append($".HasColumnType(\"{columnType}\")");
                }

                // Check for MaxLength attribute
                if (HasAttribute(property, "MaxLength", out int maxLength))
                {
                    sb.Append($".HasMaxLength({maxLength})");
                }

                if (HasAttribute(property, "Required"))
                {
                    sb.Append(".IsRequired()");
                }
                else
                {
                    // Check if the property is nullable or not, and add IsRequired() accordingly
                    if (!propertyType.IsNullable)
                    {
                        sb.Append(".IsRequired()");
                    }
                }

                // Check for HasPrecision (for decimal types)
                if (HasAttribute(property, "Precision", out int precision, out int scale))
                {
                    sb.Append($".HasPrecision({precision}, {scale})");
                }

                // Check for Default Value
                if (HasAttribute(property, "HasDefaultValue", out string defaultValue))
                {
                    sb.Append($".HasDefaultValue({defaultValue})");
                }

                // Check for Default SQL Value
                if (HasAttribute(property, "HasDefaultValueSql", out string defaultSqlValue))
                {
                    sb.Append($".HasDefaultValueSql(\"{defaultSqlValue}\")");
                }

                sb.Append($"; ");
                // Finalize the property configuration
                sb.AppendLine(); // Newline between properties
            }
            
            return sb.ToString().Trim(); // Trim to remove excess whitespace
        }

        private static bool HasAttribute(ClassProperty property, string attributeName, out string attributeValue)
        {
            attributeValue = string.Empty;
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null)
            {
                var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    attributeValue = argument.ToString().Trim('"');
                    return true;
                }
            }
            return false;
        }

        private static bool HasAttribute(ClassProperty property, string attributeName, out int maxLength)
        {
            maxLength = 0;
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null)
            {
                var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (argument != null && int.TryParse(argument.ToString(), out maxLength))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasAttribute(ClassProperty property, string attributeName, out int precision, out int scale)
        {
            precision = 0;
            scale = 0;
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 2)
            {
                var precisionArg = attribute.ArgumentList.Arguments[0];
                var scaleArg = attribute.ArgumentList.Arguments[1];

                if (int.TryParse(precisionArg.ToString(), out precision) && int.TryParse(scaleArg.ToString(), out scale))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasAttribute(ClassProperty property, string attributeName)
        {
            var attributeLists = property.propertyDeclarationSyntax.AttributeLists;
            var attributes = attributeLists.SelectMany(a => a.Attributes);
            var attributeNames = attributes.Select(a => a.Name.ToString());
            var hasAttribute = attributeNames.Any(name => name.Contains(attributeName));
            return hasAttribute;
        }
    }
}
