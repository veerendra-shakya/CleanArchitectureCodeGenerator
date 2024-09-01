using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public static class CodeFormatter
    {
        /// <summary>
        /// Reformats the provided C# code string to match Visual Studio's formatting style.
        /// </summary>
        /// <param name="code">The C# code as a string that needs to be formatted.</param>
        /// <returns>A formatted C# code string.</returns>
        public static string ReformatCode(string code)
        {
            // Create a default AdhocWorkspace
            var workspace = new AdhocWorkspace();

            // Parse the code into a syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Get the root of the syntax tree
            var root = syntaxTree.GetRoot();

            // Format the code using the default formatting options
            var formattedRoot = Formatter.Format(root, workspace);

            // Return the formatted code as a string
            return formattedRoot.ToFullString();
        }

    }
}
