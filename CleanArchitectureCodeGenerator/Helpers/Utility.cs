using CleanArchitecture.CodeGenerator.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public static class Utility
    {
        public static string CamelCaseClassName(string name)
        {

            return CamelCase(name);

        }

        public static string CamelCaseEnumValue(string name)
        {

            return CamelCase(name);

        }

        public static string CamelCasePropertyName(string name)
        {
            return CamelCase(name);

        }

        private static string CamelCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return name[0].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) + name.Substring(1);
        }

        public static string Pluralize(string name)
        {
            if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - 1) + "ies";
            }
            else
            {
                return name + "s";
            }
        }

        public static string NormalizeLineEndings(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            return Regex.Replace(content, @"\r\n|\n\r|\n|\r", "\r\n");
        }

        public static string MakeRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        public static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        public static string RelativePath_To_Namespace(string relativePath)
        {
            return relativePath.Replace(Path.DirectorySeparatorChar, '.').Replace("-", "_");
        }

        public static string SplitCamelCase(string str)
        {
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            return r.Replace(str, " ");
        }

        public static void WriteToDiskAsync(string file, string content)
        {
            // Ensure all directories in the path exist
            string directoryPath = Path.GetDirectoryName(file);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Determine the file extension
            string fileExtension = Path.GetExtension(file).ToLowerInvariant();

            // Format content based on file type
            switch (fileExtension)
            {
                case ".cs":
                    content = CodeFormatter.ReformatCode(content);
                    break;

                case ".cshtml":
                case ".razor":
                    // content = RazorPageFormatter.ReformatRazorPage(content);
                    break;

                // Add cases for other file types as needed
                // case ".html":
                //     content = FormatHtmlContent(content);
                //     break;

                default:
                    // No formatting needed
                    break;
            }


            // Write content to the file
            using (StreamWriter writer = new StreamWriter(file, false, GetFileEncoding(file)))
            {
                writer.Write(content);
            }
        }

        public static Encoding GetFileEncoding(string file)
        {
            string[] noBom = { ".cmd", ".bat", ".json" };
            string ext = Path.GetExtension(file).ToLowerInvariant();

            if (noBom.Contains(ext))
            {
                return new UTF8Encoding(false);
            }

            return new UTF8Encoding(true);
        }

        public static string[] GetParsedInput(string input)
        {
            Regex pattern = new Regex(@"[,]?([^(,]*)([\.\/\\]?)[(]?((?<=[^(])[^,]*|[^)]+)[)]?");
            List<string> results = new List<string>();
            Match match = pattern.Match(input);

            while (match.Success)
            {
                string path = match.Groups[1].Value.Trim() + match.Groups[2].Value;
                string[] extensions = match.Groups[3].Value.Split(',');

                foreach (string ext in extensions)
                {
                    string value = path + ext.Trim();

                    if (value != "" && !value.EndsWith(".", StringComparison.Ordinal) && !results.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        results.Add(value);
                    }
                }
                match = match.NextMatch();
            }
            return results.ToArray();
        }

        public static bool ValidatePath(string path, string targetDirectory)
        {
            Regex reservedFileNamePattern = new Regex($@"(?i)^(PRN|AUX|NUL|CON|COM\d|LPT\d)(\.|$)");
            HashSet<char> invalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            while (!string.IsNullOrEmpty(path))
            {
                string name = Path.GetFileName(path);

                if (reservedFileNamePattern.IsMatch(name))
                {
                    Console.WriteLine($"Invalid path: The name '{name}' is a system reserved name.");
                    return false; // The name is a system reserved name
                }

                if (name.Any(c => invalidFileNameChars.Contains(c)))
                {
                    Console.WriteLine($"Invalid path: The name '{name}' contains invalid characters.");
                    return false; // The name contains invalid characters
                }

                path = Path.GetDirectoryName(path);
            }

            // Check if the path is a directory and create it if it is
            if (!string.IsNullOrEmpty(path) && path.EndsWith("\\", StringComparison.Ordinal))
            {
                string directoryPath = Path.Combine(targetDirectory, path);
                Directory.CreateDirectory(directoryPath);
            }

            return true; // Path is valid
        }

        public static string GetProjectNameFromPath(string path)
        {
            // Use Path.GetFileName to get the last segment of the path
            string projectName = Path.GetFileName(path);

            // If the path ends with a directory separator, GetFileName will return an empty string
            // So, use GetFileName again on the trimmed path
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            return projectName;
        }


        /// <summary>
        /// Retrieves all entities from a project directory by analyzing the files.
        /// </summary>
        /// <param name="projectDirectory">The directory of the project.</param>
        /// <returns>A list of <see cref="CSharpClassObject"/> representing the entities.</returns>
        public static IEnumerable<CSharpClassObject> GetEntities(string projectDirectory)
        {
            CSharpSyntaxParser IntellisenseParser = new CSharpSyntaxParser();
            var list = new List<CSharpClassObject>();
            var _files = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
            foreach (var filePath in _files)
            {
                var objects = IntellisenseParser.ProcessFile(filePath);
                if (objects != null)
                {
                    list.AddRange(objects);
                }
            }

            return list;
        }


        // This method checks if the type is a known type (primitive types, base classes, etc.)
        public static bool IsKnownType(TypeSyntax typeSyntax)
        {
            var knownPrimitiveTypes = new HashSet<string>
                {
                    // Represents a 32-bit signed integer.
                    "int",    // Maps to `int` in the database (SQL Server).

                    // Nullable 32-bit signed integer.
                    "int?",   // Maps to `int` in the database with nullability.

                    // Represents a 64-bit signed integer.
                    "long",   // Maps to `bigint` in the database.

                    // Nullable 64-bit signed integer.
                    "long?",  // Maps to `bigint` in the database with nullability.

                    // Represents a 16-bit signed integer.
                    "short",  // Maps to `smallint` in the database.

                    // Nullable 16-bit signed integer.
                    "short?", // Maps to `smallint` in the database with nullability.

                    // Represents an 8-bit unsigned integer.
                    "byte",   // Maps to `tinyint` in the database.

                    // Nullable 8-bit unsigned integer.
                    "byte?",  // Maps to `tinyint` in the database with nullability.

                    // Represents a single-precision floating-point number.
                    "float",  // Maps to `real` in the database.

                    // Nullable single-precision floating-point number.
                    "float?", // Maps to `real` in the database with nullability.

                    // Represents a double-precision floating-point number.
                    "double", // Maps to `float` in the database.

                    // Nullable double-precision floating-point number.
                    "double?",// Maps to `float` in the database with nullability.

                    // Represents a decimal number with higher precision.
                    "decimal",// Maps to `decimal(18, 2)` in the database, with customizable precision and scale.

                    // Nullable decimal number with higher precision.
                    "decimal?", // Maps to `decimal(18, 2)` in the database with nullability.

                    // Represents a Boolean value (true or false).
                    "bool",   // Maps to `bit` in the database.

                    // Nullable Boolean value.
                    "bool?",  // Maps to `bit` in the database with nullability.

                    // Represents a single Unicode character.
                    "char",   // Maps to `nchar(1)` in the database.

                    // Nullable Unicode character.
                    "char?",  // Maps to `nchar(1)` in the database with nullability.

                    // Represents a sequence of Unicode characters (a string).
                    "string", // Maps to `nvarchar(max)` in the database by default. Length can be specified.

                    // Nullable string.
                    "string?",// Maps to `nvarchar(max)` in the database with nullability. Length can be specified.

                    // Represents a date and time.
                    "DateTime", // Maps to `datetime2` in the database, providing higher precision.

                    // Nullable date and time.
                    "DateTime?", // Maps to `datetime2` in the database with nullability.

                    // Represents a globally unique identifier.
                    "Guid", // Maps to `uniqueidentifier` in the database.

                    // Nullable globally unique identifier.
                    "Guid?", // Maps to `uniqueidentifier` in the database with nullability.


                };
            string Type = typeSyntax.ToString();
            bool isKnownType = knownPrimitiveTypes.Contains(Type);
           
            return isKnownType;
        }

        // This method checks if the type is a known base type, similar to the base classes check in the original class
        public static bool IsKnownBaseType(TypeSyntax typeSyntax)
        {
            var knownBaseTypes = new HashSet<string>
                {
                    "BaseAuditableSoftDeleteEntity", "BaseAuditableEntity", "BaseEntity", "IEntity", "ISoftDelete"
                };

            return knownBaseTypes.Contains(typeSyntax.ToString());
        }

        public static bool IsKnownBaseType(string typeName)
        {
            var knownBaseTypes = new HashSet<string>
                {
                    "BaseAuditableSoftDeleteEntity", "BaseAuditableEntity", "BaseEntity", "IEntity", "ISoftDelete"
                };

            return knownBaseTypes.Contains(typeName);
        }


        public static bool IsPropertyNameValid(string propertyName)
        {
            // Rule 1: Check if the name is in CamelCase
            if (!IsCamelCase(propertyName))
            {
                return false;
            }

            // Rule 2: Check for spaces, hyphens, or underscores
            if (propertyName.Contains(' ') || propertyName.Contains('-') || propertyName.Contains('_'))
            {
                return false;
            }

            //// Rule 3: Check if the name is singular (simple heuristic, assuming singular form is just checking for 's' at the end)
            //if (propertyName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            //{
            //    return false;
            //}

            return true;
        }

        public static bool IsCamelCase(string input)
        {
            // Check if the first character is uppercase and the rest do not contain spaces or special characters
            return char.IsUpper(input[0]) && input.Skip(1).All(c => !char.IsWhiteSpace(c) && c != '-' && c != '_');
        }

        public static bool IsModelClassValid(CSharpClassObject classObject)
        {
            // Rule 1: Check if the name is in CamelCase
            if (!IsCamelCase(classObject.Name))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: The Entity/Model class name must be in CamelCase.");
                Console.ResetColor();
                return false;
            }

            // Rule 2: Check for spaces, hyphens, or underscores
            if (classObject.Name.Contains(' ') || classObject.Name.Contains('-') || classObject.Name.Contains('_'))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: The Entity/Model class name must not contain spaces, hyphens, or underscores.");
                Console.ResetColor();
                return false;
            }

            // Rule 3: Check if the name is singular (simple heuristic, assuming singular form is just checking for 's' at the end)
            if (classObject.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: The Entity/Model class name should be singular and not end with 's'.");
                Console.ResetColor();
                return false;
            }

            // Rule 4: Check if the base type is known
            if (!IsKnownBaseType(classObject.BaseName))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: The Entity/Model class base type is invalid. It should be one of the following: \"BaseAuditableSoftDeleteEntity\", \"BaseAuditableEntity\", \"BaseEntity\", \"IEntity\", \"ISoftDelete\".");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        public static bool ValidateClassProperties(CSharpClassObject classObject)
        {
            foreach (var property in classObject.Properties)
            {
                var typeSyntax = property.propertyDeclarationSyntax.Type;

                if (property.Type.IsList || property.Type.IsDictionary || property.Type.IsICollection || property.Type.IsIEnumerable)
                {
                    return true;
                }

                if (!IsKnownType(typeSyntax))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: The type '{typeSyntax}' of property '{property.PropertyName}' is not a known type.");
                    Console.ResetColor();
                    return false;
                }

                if (!IsPropertyNameValid(property.PropertyName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: The property name '{property.PropertyName}' is not valid.");
                    Console.ResetColor();
                    return false;
                }
            }
            return true;
        }

        public static List<string> GetTemplateFiles()
        {
            List<string> _templateFiles = new List<string>();
            string _defaultExt = ".txt";
            var assembly = Assembly.GetExecutingAssembly().Location;
            var _template_folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
            _templateFiles.AddRange(Directory.GetFiles(_template_folder, "*" + _defaultExt, SearchOption.AllDirectories));
            return _templateFiles.ToList();
        }
        /// <summary>
        /// Search and Returns Template File Path
        /// </summary>
        /// <param name="relative">Domain\Events\</param>
        /// <param name="file">D:\CleanArchitectureWithBlazorServer-main\src\Domain\Events\CustomerCreatedEvent.cs</param>
        /// <returns></returns>
        public static string GetTemplateFile(string relative, string file)
        {
            string _defaultExt = ".txt";
            var list = GetTemplateFiles();
            var templateFolders = new[]
            {
                "Commands\\AcceptChanges",
                "Commands\\Create",
                "Commands\\Delete",
                "Commands\\Update",
                "Commands\\AddEdit",
                "Commands\\Import",
                "DTOs",
                "Caching",
                "EventHandlers",
                "Events",
                "Specification",
                "Queries\\Export",
                "Queries\\GetAll",
                "Queries\\GetById",
                "Queries\\Pagination",
                "Pages",
                "Pages\\Components",
                "Persistence\\Configurations",
                "PermissionSet",
                "Entities",
            };

            var extension = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);
            var safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);

            // Determine the folder pattern based on the relative path
            var folderPattern = templateFolders
                .FirstOrDefault(x => relative.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)
                ?.Replace("\\", "\\\\");

            if (!string.IsNullOrEmpty(folderPattern))
            {
                // Look for direct file name matches in the specified template folder
                var matchingFile = list
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault(f => Regex.IsMatch(f, folderPattern, RegexOptions.IgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(f).Split('.')
                                         .All(x => name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0));

                if (!string.IsNullOrEmpty(matchingFile))
                {
                    return matchingFile;
                }
            }

            // If no direct match, look for file extension matches
            var extensionMatch = list
                .FirstOrDefault(f => Path.GetFileName(f).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase) &&
                                     File.Exists(f));

            if (extensionMatch != null)
            {
                var adjustedName = AdjustForSpecific(safeName, extension);
                return Path.Combine(Path.GetDirectoryName(extensionMatch), adjustedName + _defaultExt);
            }

            // If no match is found, return null or throw an exception as per your requirement
            return null;
        }

        private static string AdjustForSpecific(string safeName, string extension)
        {
            if (Regex.IsMatch(safeName, "^I[A-Z].*"))
            {
                return extension += "-interface";
            }

            return extension;
        }
    }

}
