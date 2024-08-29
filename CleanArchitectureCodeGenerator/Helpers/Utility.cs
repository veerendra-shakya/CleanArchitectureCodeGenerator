using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public static async Task WriteToDiskAsync(string file, string content)
        {
            // Ensure all directories in the path exist
            string directoryPath = Path.GetDirectoryName(file);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write content to the file
            using (StreamWriter writer = new StreamWriter(file, false, GetFileEncoding(file)))
            {
                await writer.WriteAsync(content);
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
        /// <returns>A list of <see cref="IntellisenseObject"/> representing the entities.</returns>
        public static IEnumerable<IntellisenseObject> GetEntities(string projectDirectory)
        {
            var list = new List<IntellisenseObject>();
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

    }

}
