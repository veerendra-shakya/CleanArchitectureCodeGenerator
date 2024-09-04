using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Linq;
using System.Text;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class FluentValidationGenerator
    {
        public static string GenerateFluentValidation(CSharpClassObject classObject)
        {
            var sb = new StringBuilder();

            foreach (var property in classObject.Properties)
            {
                var propertyName = property.PropertyName;
                sb.Append($"RuleFor(v => v.{propertyName})");

                if (HasAttribute(property, "Required"))
                {
                    sb.Append(".NotEmpty()");
                    sb.Append(".WithMessage(\"This field is required and cannot be empty.\")");
                }

                if (HasAttribute(property, "MaxLength", out int maxLength))
                {
                    sb.Append($".MaximumLength({maxLength})");
                    sb.Append($".WithMessage(\"This field must not exceed {maxLength} characters.\")");
                }

                if (HasAttribute(property, "Range", out int minValue, out int maxValue))
                {
                    sb.Append($".InclusiveBetween({minValue}, {maxValue})");
                    sb.Append($".WithMessage(\"This field must be greater than {minValue} and less than {maxValue}.\")");
                }

                if (HasAttribute(property, "RegularExpression", out string regexPattern, out string errorMessage))
                {
                    if (regexPattern.StartsWith("@\""))
                    {
                        regexPattern = regexPattern.Replace("@\"", "");
                    }

                    sb.Append($".Matches(@\"{regexPattern}\")");
                    sb.Append($".WithMessage(\"{errorMessage}\")");
                }

                sb.Append("; ");
                sb.AppendLine(); // Add a new line after each property validation rule set

            }

            return sb.ToString().Trim(); // Trim to remove unnecessary line breaks
        }

        private static bool HasAttribute(ClassProperty property, string attributeName)
        {
            var attributeLists = property.propertyDeclarationSyntax.AttributeLists;
            var attributes = attributeLists.SelectMany(a => a.Attributes);
            var attributeNames = attributes.Select(a => a.Name.ToString());
            var hasAttribute = attributeNames.Any(name => name.Contains(attributeName));
            return hasAttribute;
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

        private static bool HasAttribute(ClassProperty property, string attributeName, out int minValue, out int maxValue)
        {
            minValue = 0;
            maxValue = 0;
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 2)
            {
                var minArg = attribute.ArgumentList.Arguments[0];
                var maxArg = attribute.ArgumentList.Arguments[1];

                if (int.TryParse(minArg.ToString(), out minValue) && int.TryParse(maxArg.ToString(), out maxValue))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasAttribute(ClassProperty property, string attributeName, out string regexPattern)
        {
            regexPattern = string.Empty;
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null)
            {
                var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    regexPattern = argument.ToString().Trim('"');
                    return true;
                }
            }
            return false;
        }

        private static bool HasAttribute(ClassProperty property, string attributeName, out string regexPattern, out string errorMessage)
        {
            regexPattern = string.Empty;
            errorMessage = string.Empty;

            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains(attributeName));

            if (attribute != null)
            {
                // Extract the regex pattern
                var regexArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (regexArgument != null)
                {
                    regexPattern = regexArgument.ToString().Trim('"');
                }

                // Extract the error message, if present
                var errorMessageArgument = attribute.ArgumentList?.Arguments
                    .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "ErrorMessage");

                if (errorMessageArgument != null)
                {
                    errorMessage = errorMessageArgument.Expression.ToString().Trim('"');
                }

                return true;
            }
            return false;
        }

    }
}
