using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Humanizer;
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
                if (property.DataUsesAtt.PrimaryRole == "Relationship" && !property.DataUsesAtt.IsForeignKey)
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

                    if(HasAttribute(property, "DataFormat"))
                    {
                        HandleDataFormat(sb, property);
                       // continue; // Skip further processing for this property
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

        #region Code Generator Functions
        private static void GenerateRelationshipFluentApi(StringBuilder sb, ClassProperty property)
        {
            var relatedEntity = property.Type.TypeName; // The type name of the related entity
            var relationshipType = property.DataUsesAtt.RelationshipType;
            var deleteBehavior = property.DataUsesAtt.DeleteBehavior; // No default behavior, check if it's null

            switch (relationshipType)
            {
                case "OneToOne":
                    // Generate One-to-One relationship configuration
                    sb.AppendLine($"// One-to-One relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasOne(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithOne(p => p.{property.DataUsesAtt.InverseProperty})")
                      .AppendLine($"    .HasForeignKey<{property.Type.TypeName}>(p => p.{property.DataUsesAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude(false);");
                    break;

                case "OneToMany":
                    // Generate One-to-Many relationship configuration
                    sb.AppendLine($"// One-to-Many relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasMany(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithOne(p => p.{property.DataUsesAtt.InverseProperty})")
                      .AppendLine($"    .HasForeignKey(p => p.{property.DataUsesAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude(false);");
                    break;

                case "ManyToOne":
                    // Generate Many-to-One relationship configuration
                    sb.AppendLine($"// Many-to-One relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasOne(e  => e.{property.PropertyName})")
                      .AppendLine($"    .WithMany(p => p.{property.DataUsesAtt.InverseProperty})") 
                      .AppendLine($"    .HasForeignKey(p => p.{property.DataUsesAtt.ForeignKeyProperty})");

                    // Conditionally add OnDelete if deleteBehavior is not null
                    if (!string.IsNullOrEmpty(deleteBehavior))
                    {
                        sb.AppendLine($"    .OnDelete(DeleteBehavior.{deleteBehavior})");
                    }
                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.Append("; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude(false);");
                    break;

                case "ManyToMany":
                    // Generate Many-to-Many relationship configuration
                    sb.AppendLine($"// Many-to-Many relationship with {property.PropertyName}");
                    sb.AppendLine($"builder.HasMany(e => e.{property.PropertyName})")
                      .AppendLine($"    .WithMany(p => p.{property.DataUsesAtt.InverseProperty})")
                      .AppendLine($"    .UsingEntity<{property.DataUsesAtt.LinkingTable}>(")
                      .AppendLine($"        j => j.HasOne(y => y.{property.PropertyNameSingular})")
                      .AppendLine($"              .WithMany()")
                      .AppendLine($"              .HasForeignKey(x => x.{property.PropertyNameSingular}Id),")
                      .AppendLine($"        j => j.HasOne(y => y.{property.DataUsesAtt.InverseProperty.Singularize()})")
                      .AppendLine($"              .WithMany()")
                      .AppendLine($"              .HasForeignKey(x => x.{property.DataUsesAtt.InverseProperty.Singularize()}Id))");

                    sb.Length -= Environment.NewLine.Length;  // Removes the last newline
                    sb.AppendLine($"; ");
                    sb.AppendLine("\n");
                    // Add AutoInclude() for the navigation property
                    //sb.AppendLine($"#warning Disable auto-inclusion on one side of a many-to-many relationship to avoid cyclic references with 'AutoInclude(false)'.");
                    sb.AppendLine($"builder.Navigation(e => e.{property.PropertyName}).AutoInclude(false);");
                    break;

                default:
                    sb.AppendLine("// Relationship type not recognized.");
                    break;
            }
        }

        private static void HandleJsonFileOrImage(StringBuilder sb, ClassProperty property)
        {
            if (property.Type.TypeName.Contains("List"))
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

        private static void HandleDataFormat(StringBuilder sb, ClassProperty property)
        {
            var attribute = property.propertyDeclarationSyntax.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("DataFormat"));

            if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 1)
            {
                var arg = attribute.ArgumentList.Arguments[0].ToString(); // gets argument as string

                // Remove DataFormat. prefix if present
                if (arg.StartsWith("DataFormat."))
                {
                    arg = arg.Replace("DataFormat.", string.Empty);
                }

                switch (arg)
                {
                    case "Decimal_18_2":
                        sb.Append(".HasColumnType(\"decimal(18, 2)\")");
                        break;

                    case "String_30":
                        sb.Append(".HasColumnType(\"varchar(30)\")");
                        break;
                    case "String_50":
                        sb.Append(".HasColumnType(\"varchar(50)\")");
                        break;

                    case "String_100":
                        sb.Append(".HasColumnType(\"varchar(100)\")");
                        break;

                    case "String_160":
                        sb.Append(".HasColumnType(\"varchar(160)\")");
                        break;

                    case "String_255":
                        sb.Append(".HasColumnType(\"varchar(255)\")");
                        break;

                    case "String_500":
                        sb.Append(".HasColumnType(\"varchar(500)\")");
                        break;

                    case "String_5000":
                        sb.Append(".HasColumnType(\"varchar(5000)\")");
                        break;
                    case "String_Max":
                        sb.Append(".HasColumnType(\"text\")");
                        break;

                    case "Json_String":
                        HandleJsonString(sb, property);
                        break;

                    default:
                        sb.Append(".HasColumnType(\"varchar(50)\")");
                        break;
                }

            }
        }

        private static void HandleJsonString(StringBuilder sb, ClassProperty property)
        {
            if (property.Type.TypeName.Contains("List"))
            {
                sb
                  //.AppendLine($"builder.Property(e => e.{property.PropertyName})")
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
                sb
                  //.AppendLine($"builder.Property(e => e.{property.PropertyName})")
                  .AppendLine($".HasConversion(")
                  .AppendLine($"    v => JsonSerializer.Serialize(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    v => JsonSerializer.Deserialize<{property.Type.TypeName.Replace("?", "")}>(v, DefaultJsonSerializerOptions.Options),")
                  .AppendLine($"    new ValueComparer<{property.Type.TypeName.Replace("?", "")}>(")
                  .AppendLine($"        (c1, c2) => c1.Equals(c2),")
                  .AppendLine($"        c => c == null ? 0 : c.GetHashCode(),")
                  .AppendLine($"        c => c == null ? null : JsonSerializer.Deserialize<{property.Type.TypeName.Replace("?", "")}>(JsonSerializer.Serialize(c, DefaultJsonSerializerOptions.Options), DefaultJsonSerializerOptions.Options)));");

            }


        }


        #endregion

        #region Helper Functions
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
        #endregion

   
    }
}
