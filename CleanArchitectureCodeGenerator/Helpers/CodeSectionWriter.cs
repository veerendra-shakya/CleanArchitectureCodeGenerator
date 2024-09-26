using System.Text;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.Helpers;

public class CodeSectionWriter
{
    public static void AppendCodeToSection(string filePath, string sectionId, string newCode)
    {
        // Read the file contents
        string fileContent = File.ReadAllText(filePath);

        // Define the regex pattern to find the section with the given ID
        string sectionPattern = $@"(/\* <generated-section:{sectionId}>)(.*?)(/\* </generated-section:{sectionId}>\s*\*/)";

        // Match the section using the regex
        var regex = new Regex(sectionPattern, RegexOptions.Singleline);
        var match = regex.Match(fileContent);

        if (match.Success)
        {
            // Get the content of the section
            string sectionStart = match.Groups[1].Value; // Start marker
            string sectionEnd = match.Groups[3].Value; // End marker
            string existingCode = match.Groups[2].Value.Trim(); // Existing code inside the section

            // Append the new code to the existing code
            string updatedCode = $"{existingCode}\n{newCode}";

            // Build the updated section
            string updatedSection = $"{sectionStart}\n{updatedCode}\n{sectionEnd}";

            // Replace the old section with the updated one
            fileContent = regex.Replace(fileContent, updatedSection);

            // Write the updated content back to the file
            File.WriteAllText(filePath, fileContent);
        }
        else
        {
            // Section not found, append the new section at the end of the file
            StringBuilder sb = new StringBuilder(fileContent);
            sb.AppendLine($"\n/* <generated-section:{sectionId}>");
            sb.AppendLine("   Generated code, modifications allowed");
            sb.AppendLine("*/");
            sb.AppendLine(newCode);
            sb.AppendLine($"/* </generated-section:{sectionId}> */");

            // Write the updated content back to the file
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}