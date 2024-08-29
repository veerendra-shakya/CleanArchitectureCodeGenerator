using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator;

namespace CleanArchitectureCodeGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var projectRoot = @"C:\path\to\your\project";
            //var filePath = @"C:\path\to\your\project\MyClass.cs";
            //var itemName = "MyClass";
            //var selectFolder = @"C:\path\to\your\project\SelectedFolder";

            //var classObject = new IntellisenseObject
            //{
            //    Name = "MyClass",
            //    Namespace = "MyNamespace",
            //    Properties = new List<IntellisenseProperty>
            //{
            //    new IntellisenseProperty
            //    {
            //        Name = "Id",
            //        Type = new IntellisenseType { CodeName = "int" }
            //    },
            //    new IntellisenseProperty
            //    {
            //        Name = "Name",
            //        Type = new IntellisenseType { CodeName = "string" }
            //    }
            //}
            //};

            // string templateContent = await TemplateMap.GetTemplateFilePathAsync(projectRoot, classObject, filePath, itemName, selectFolder);

            //Console.WriteLine(templateContent);

            // Run the code generator.
            await CodeGenerator.RunAsync();
        }
    }
}
