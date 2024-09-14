using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class Ef_FluentConfigurationsGenerator
    {
        public static string GenerateConfigurations(CSharpClassObject classObject)
        {
            var sb = new StringBuilder();

            foreach (var property in classObject.Properties)
            {
                var propertyName = property.PropertyName;
                var propertyType = property.Type;

                // Start the builder for the property configuration
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    // Handle relationships
                    GenerateRelationshipFluentApi(sb, property);
                    sb.AppendLine("\n"); // Newline between properties
                }
                else
                {

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

    }
}
