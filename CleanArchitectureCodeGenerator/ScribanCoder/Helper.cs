using CleanArchitecture.CodeGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Console.WriteLine($"Error: Invalid Path '{targetFile.FullName}'.");
                Console.ResetColor();
                return null;
            }


            if (targetFile.Exists)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"The file '{targetFile.FullName}' already exists.");
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

    }
}
