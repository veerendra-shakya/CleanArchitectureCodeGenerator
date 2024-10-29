using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class Ef_FluentConfigurationsGenerator
    {

        public static string GenerateConfigurations(CSharpClassObject classObject)
        {
            var sb = new StringBuilder();

            foreach (var property in classObject.ClassProperties)
            {
                var propertyName = property.PropertyName;
                var propertyType = property.Type;

                // Start the builder for the property configuration
                if (property.ScaffoldingAtt.PropRole == "Relationship" && !property.ScaffoldingAtt.IsForeignKey)
                {
                    // Handle relationships
                    GenerateRelationshipFluentApi(sb, property);
                    sb.AppendLine("\n"); // Newline between properties
                }
                else if (propertyType.TypeName.Contains("JsonImage") || propertyType.TypeName.Contains("JsonFile"))
                {
                    HandleJsonFileOrImage(sb, property);
                    continue; // Skip further processing for this property
                }
                else
                {

                    // Start the builder for the property configuration
                    sb.Append($"builder.Property(x => x.{propertyName})");

                    if(propertyType.TypeName.Contains("Enum"))
                    {
                        sb.Append($".HasConversion<string>()");
                    }
                    if(property.Type.IsList)
                    {
                        sb.Append($".HasStringListConversion()");
                    }
                    if (property.Type.IsDictionary)
                    {
                        sb.Append($".HasJsonConversion()");
                    }

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

                    if(HasAttribute(property, "DataType"))
                    {
                        HandleDataType(sb, property);
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
                    sb.AppendLine("\n"); // Newline between properties
                }

            }
            
            return sb.ToString().Trim(); // Trim to remove excess whitespace
        }

        private static void GenerateRelationshipFluentApi(StringBuilder sb, ClassProperty property)
        {
            var relatedEntity = property.Type.TypeName; // The type name of the related entity
            var relationshipType = property.ScaffoldingAtt.RelationshipType;
            var deleteBehavior = property.ScaffoldingAtt.DeleteBehavior; // No default behavior, check if it's null

            switch (relationshipType)
            {
                case "OneToOne":
                    // Generate One-to-One relationship configuration
                    sb.AppendLine($"// One-to-One relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasOne(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithOne(p => p.{property.ScaffoldingAtt.InverseProperty})")
                      .AppendLine($"    .HasForeignKey<{property.Type.TypeName}>(p => p.{property.ScaffoldingAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude();");
                    break;

                case "OneToMany":
                    // Generate One-to-Many relationship configuration
                    sb.AppendLine($"// One-to-Many relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasMany(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithOne(p => p.{property.ScaffoldingAtt.InverseProperty})")
                      .AppendLine($"    .HasForeignKey(p => p.{property.ScaffoldingAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude();");
                    break;

                case "ManyToOne":
                    // Generate Many-to-One relationship configuration
                    sb.AppendLine($"// Many-to-One relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasOne(e  => e.{property.PropertyName})")
                      .AppendLine($"    .WithMany(p => p.{property.ScaffoldingAtt.InverseProperty})") 
                      .AppendLine($"    .HasForeignKey(p => p.{property.ScaffoldingAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude();");
                    break;

                case "ManyToMany":
                    // Generate Many-to-Many relationship configuration
                    sb.AppendLine($"// Many-to-Many relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasMany(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithMany(p => p.{property.ScaffoldingAtt.InverseProperty})")
                      .AppendLine($"    .UsingEntity<{property.ScaffoldingAtt.LinkingTable}>(")
                      .AppendLine($"        j => j.HasOne(y => y.{property.PropertyName.Singularize()})")
                      .AppendLine($"              .WithMany()")
                      .AppendLine($"              .HasForeignKey(x => x.{property.PropertyName.Singularize()}Id),")
                      .AppendLine($"        j => j.HasOne(y => y.{property.ScaffoldingAtt.InverseProperty.Singularize()})")
                      .AppendLine($"              .WithMany()")
                      .AppendLine($"              .HasForeignKey(x => x.{property.ScaffoldingAtt.InverseProperty.Singularize()}Id))");

                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.AppendLine($"; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"#warning Disable auto-inclusion on one side of a many-to-many relationship to avoid cyclic references with 'AutoInclude(false)'.");
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude();");
                    break;

                default:
                    sb.AppendLine("// Relationship type not recognized.");
                    break;
            }
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

        private static void HandleJsonFileOrImage(StringBuilder sb, ClassProperty property)
        {
            if(property.Type.TypeName.Contains("List"))
            {
                sb.AppendLine($"builder.Property(e => e.{property.PropertyName})")
                  .AppendLine($".HasConversion(")
                  .AppendLine($"    v => JsonSerializer.Serialize(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    v => JsonSerializer.Deserialize<{property.Type.TypeName.Replace("?", "")}>(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    new ValueComparer<{property.Type.TypeName.Replace("?", "")}>(")
                  .AppendLine($"        (c1, c2) => c1.SequenceEqual(c2),")
                  .AppendLine($"        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),")
                  .AppendLine($"        c => c.ToList()));");
            }
            else
            {
                sb.AppendLine($"builder.Property(e => e.{property.PropertyName})")
                  .AppendLine($".HasConversion(")
                  .AppendLine($"    v => JsonSerializer.Serialize(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    v => JsonSerializer.Deserialize<{property.Type.TypeName.Replace("?", "")}>(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    new ValueComparer<{property.Type.TypeName.Replace("?", "")}>(")
                  .AppendLine($"        (c1, c2) => c1.Equals(c2),")
                  .AppendLine($"        c => c == null ? 0 : c.GetHashCode(),")
                  .AppendLine($"        c => c == null ? null : JsonSerializer.Deserialize<{property.Type.TypeName.Replace("?", "")}>(JsonSerializer.Serialize(c, DefaultJsonSerializerOptions.Options), DefaultJsonSerializerOptions.Options)));");

            }

            sb.AppendLine("\n");
        }

        private static void HandleDataType(StringBuilder sb, ClassProperty property)
        {
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("DataType"));

            if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 1)
            {
                var arg = attribute.ArgumentList.Arguments[0].ToString(); // gets argument as string

                // Remove DataType. prefix if present
                if (arg.StartsWith("DataType."))
                {
                    arg = arg.Replace("DataType.", string.Empty);
                }

                switch (arg)
                {
                    case "Currency":
                        sb.Append(".HasColumnType(\"decimal(18, 2)\")");
                        break;

                    case "Date":
                        sb.Append(".HasColumnType(\"date\")");
                        break;

                    case "DateTime":
                        sb.Append(".HasColumnType(\"datetime\")");
                        break;

                    case "Time":
                        sb.Append(".HasColumnType(\"time\")");
                        break;

                    case "PhoneNumber":
                        sb.Append(".HasColumnType(\"varchar(15)\")");
                        break;

                    case "EmailAddress":
                        sb.Append(".HasColumnType(\"varchar(255)\")");
                        break;

                    case "Url":
                        sb.Append(".HasColumnType(\"varchar(2083)\")"); // Max URL length in most browsers
                        break;

                    case "CreditCard":
                        sb.Append(".HasColumnType(\"varchar(19)\")");
                        break;

                    case "PostalCode":
                        sb.Append(".HasColumnType(\"varchar(10)\")");
                        break;

                    case "Html":
                        sb.Append(".HasColumnType(\"text\")"); // Long text type for HTML content
                        break;

                    case "Text":
                        sb.Append(".HasColumnType(\"nvarchar(max)\")"); // Supports large text content
                        break;

                    case "MultilineText":
                        sb.Append(".HasColumnType(\"nvarchar(max)\")"); // Allows for multi-line text data
                        break;

                    case "Upload":
                        sb.Append(".HasColumnType(\"varbinary(max)\")"); // Suitable for file uploads
                        break;

                    case "Password":
                        sb.Append(".HasColumnType(\"nvarchar(255)\")"); // Allows storage of encrypted passwords
                        break;

                    case "Custom":
                    case "Duration":
                        sb.Append(".HasColumnType(\"time\")"); // Duration as a time value
                        break;

                    default:
                        sb.Append(".HasColumnType(\"nvarchar(max)\")"); // Default for unspecified data types
                        break;
                }

            }
        }
    }
}
