using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder
{
    public static class Helper
    {
        public static FileInfo? GetFileInfo(string relativeTargetPath, string targetProjectDirectory)
        {
            FileInfo targetFile = new FileInfo(Path.Combine(targetProjectDirectory, relativeTargetPath));

            if (!Utility.ValidatePath(relativeTargetPath, targetProjectDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Invalid Path '{relativeTargetPath}'.");
                Console.ResetColor();
                return null;
            }


            if (targetFile.Exists)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"The file '{relativeTargetPath}' already exists.");
                Console.ResetColor();
                return null;
            }

            return targetFile;
        }

        public static string GetNamespace(string relativePath)
        {
            var NamespaceName = ApplicationHelper.RootNamespace;
            if (!string.IsNullOrEmpty(relativePath))
            {
                NamespaceName += "." + Utility.RelativePath_To_Namespace(relativePath);
            }
            NamespaceName = NamespaceName.TrimEnd('.');
            return NamespaceName;
        }

        public static string CreateCommandFieldDefinition(CSharpClassObject classObject)
        {
            var output = new StringBuilder();
            //.Where(x => x.Type.IsKnownType)
            foreach (var property in classObject.ClassProperties)
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

        public static string ExtractDataType(string genericString)
        {
            // Use Regex to match the text inside the < and >
            var match = Regex.Match(genericString, @"<([^>]+)>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty; // Return empty if no match found
        }

    }
}
