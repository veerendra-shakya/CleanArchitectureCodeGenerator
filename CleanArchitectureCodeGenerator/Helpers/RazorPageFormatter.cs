using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public static class RazorPageFormatter
    {
        /// <summary>
        /// Reformats a Razor page content by formatting C# code blocks using Roslyn
        /// and applying basic indentation rules to the HTML/CSS/JavaScript.
        /// </summary>
        /// <param name="razorContent">The content of the Razor page as a string.</param>
        /// <returns>A formatted Razor page content as a string.</returns>
        public static string ReformatRazorPage(string razorContent)
        {
            // Split the content into lines
            string[] lines = razorContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // Determine the range of HTML content
            (int htmlStartLine, int htmlEndLine) = FindHtmlRange(lines);

            // Extract and format the HTML content within the identified range
            string formattedHtmlContent = FormatHtmlContent(GetContentRange(lines, htmlStartLine, htmlEndLine));

            // Replace the original HTML content with the formatted HTML content
            ReplaceContentRange(ref lines, htmlStartLine, htmlEndLine, formattedHtmlContent);

            // Rejoin the lines into a single string
            string formattedRazorContent = string.Join(Environment.NewLine, lines);

            // Regex to match C# code blocks in Razor pages (@code, @functions, etc.)
            var codeBlockRegex = new Regex(@"(@code|@functions)\s*{[^}]*}", RegexOptions.Singleline);

            // Replace each C# code block with formatted code
            formattedRazorContent = codeBlockRegex.Replace(formattedRazorContent, match =>
            {
                string codeBlock = match.Value;
                string formattedCodeBlock = CodeFormatter.ReformatCode(codeBlock); // Format the C# code inside the block
                return formattedCodeBlock;
            });

            // Remove multiple line gaps
            formattedRazorContent = RemoveMultipleLineGaps(formattedRazorContent);

            return formattedRazorContent;
        }

        /// <summary>
        /// Determines the starting and ending lines of the HTML content.
        /// </summary>
        /// <param name="lines">The content split into lines.</param>
        /// <returns>A tuple with the start and end line numbers of the HTML content.</returns>
        private static (int htmlStartLine, int htmlEndLine) FindHtmlRange(string[] lines)
        {
            int htmlStartLine = -1;
            int htmlEndLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Identify the start of HTML content
                if (htmlStartLine == -1 && line.StartsWith("<"))
                {
                    htmlStartLine = i;
                }

                // Identify the end of HTML content just before a @code block
                if (line.StartsWith("@code"))
                {
                    htmlEndLine = i - 1;
                    break;
                }
            }

            // If no @code block is found, consider the last line as the end of HTML
            if (htmlEndLine == -1)
            {
                htmlEndLine = lines.Length - 1;
            }

            return (htmlStartLine, htmlEndLine);
        }

        /// <summary>
        /// Extracts the content from the specified range of lines.
        /// </summary>
        /// <param name="lines">The content split into lines.</param>
        /// <param name="startLine">The starting line of the range.</param>
        /// <param name="endLine">The ending line of the range.</param>
        /// <returns>The extracted content as a single string.</returns>
        private static string GetContentRange(string[] lines, int startLine, int endLine)
        {
            return string.Join(Environment.NewLine, lines, startLine, endLine - startLine + 1);
        }

        /// <summary>
        /// Replaces the content within the specified range of lines.
        /// </summary>
        /// <param name="lines">The content split into lines.</param>
        /// <param name="startLine">The starting line of the range.</param>
        /// <param name="endLine">The ending line of the range.</param>
        /// <param name="newContent">The new content to replace the old content with.</param>
        private static void ReplaceContentRange(ref string[] lines, int startLine, int endLine, string newContent)
        {
            var newLines = newContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < newLines.Length; i++)
            {
                lines[startLine + i] = newLines[i];
            }

            // Clear the remaining lines if the new content is shorter
            for (int i = startLine + newLines.Length; i <= endLine; i++)
            {
                lines[i] = string.Empty;
            }
        }

        /// <summary>
        /// Applies advanced formatting to HTML content using HtmlAgilityPack.
        /// </summary>
        /// <param name="htmlContent">The HTML content as a string.</param>
        /// <returns>The formatted HTML content as a string.</returns>
        private static string FormatHtmlContent(string htmlContent)
        {
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            // Create a StringWriter to capture the formatted output
            using (var writer = new System.IO.StringWriter())
            {
                document.Save(writer);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Removes multiple consecutive empty lines and ensures there is only a single empty line between blocks.
        /// </summary>
        /// <param name="content">The content as a string.</param>
        /// <returns>The content with reduced line gaps.</returns>
        private static string RemoveMultipleLineGaps(string content)
        {
            // Regex to find multiple line breaks
            var multipleLineGapsRegex = new Regex(@"\n\s*\n\s*\n", RegexOptions.Multiline);

            // Replace multiple line breaks with a single line break
            return multipleLineGapsRegex.Replace(content, Environment.NewLine + Environment.NewLine);
        }
    }
}
