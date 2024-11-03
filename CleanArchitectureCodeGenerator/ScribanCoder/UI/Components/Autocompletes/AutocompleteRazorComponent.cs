using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.UI.Components.Autocompletes;

public static class AutocompleteRazorComponent
{
    public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory, bool force = false)
    {
        if (!Helper.IsValidModel(modalClassObject)) return;
        FileInfo? targetFile = Helper.GetFileInfo(relativeTargetPath, targetProjectDirectory, force);
        if (targetFile == null)
        {
            return;
        }

        try
        {
            var relativePath = Utility.MakeRelativePath(ApplicationHelper.RootDirectory, Path.GetDirectoryName(targetFile.FullName) ?? "");
            string templateFilePath = Utility.GetTemplateFile(relativePath, targetFile.FullName);
            string templateContent = File.ReadAllText(templateFilePath, Encoding.UTF8);
            string NamespaceName = Helper.GetNamespace(relativePath);

            string querystring = ComposeQueryString(modalClassObject);
            string returnstring = ComposeReturnString(modalClassObject);
            // Initialize MasterData object
            var masterdata = new
            {
                rootdirectory = ApplicationHelper.RootDirectory,
                rootnamespace = ApplicationHelper.RootNamespace,
                namespacename = NamespaceName,
                domainprojectname = ApplicationHelper.DomainProjectName,
                uiprojectname = ApplicationHelper.UiProjectName,
                infrastructureprojectname = ApplicationHelper.InfrastructureProjectName,
                applicationprojectname = ApplicationHelper.ApplicationProjectName,
                domainprojectdirectory = ApplicationHelper.DomainProjectDirectory,
                infrastructureprojectdirectory = ApplicationHelper.InfrastructureProjectDirectory,
                uiprojectdirectory = ApplicationHelper.UiProjectDirectory,
                applicationprojectdirectory = ApplicationHelper.ApplicationProjectDirectory,
                modelnameplural = modalClassObject.NamePlural,
                modelname = modalClassObject.Name,
                querystring,
                returnstring,
            };

            // Parse and render the class template
            var classTemplate = Template.Parse(templateContent);
            string generatedClass = classTemplate.Render(masterdata);

            if (!string.IsNullOrEmpty(generatedClass))
            {
                Utility.WriteToDiskAsync(targetFile.FullName, generatedClass);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Created file: {relativeTargetPath}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error generating file '{relativeTargetPath}': {ex.Message}");
            Console.ResetColor();
        }
    }



    private static string ComposeQueryString(CSharpClassObject model)
    {
        var masterProperty = model.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
        var displayProperties = model.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

        if (masterProperty != null)
        {
            // Insert masterProperty at the beginning of the list
            displayProperties.Insert(0, masterProperty);
        }

        var sb = new StringBuilder();

        // Start composing the query string dynamically
        sb.Append($"result = _list_{model.Name}")
          .AppendLine()
          .Append("    .Where(x => $\"");

        for (int i = 0; i < displayProperties.Count; i++)
        {
            var property = displayProperties[i];
            sb.Append($"{{x.{property.PropertyName}}}");

            // Append a " - " separator, but not after the last property
            if (i < displayProperties.Count - 1)
            {
                sb.Append(" - ");
            }
        }

        sb.AppendLine("\"")
          .Append("    .Contains(value, StringComparison.OrdinalIgnoreCase))")
          .AppendLine()
          .Append("    .Select(x => (Guid)x.Id).ToList();");

        return sb.ToString();
    }

    private static string ComposeReturnString(CSharpClassObject model)
    {
        var masterProperty = model.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
        var displayProperties = model.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

        if (masterProperty != null)
        {
            // Insert masterProperty at the beginning of the list
            displayProperties.Insert(0, masterProperty);
        }

        var sb = new StringBuilder();

        sb.Append("\"");

        // Loop through the display properties to generate the return string
        for (int i = 0; i < displayProperties.Count; i++)
        {
            var property = displayProperties[i];
            sb.Append($"{{userDto?.{property.PropertyName}}}");

            // Append a " - " separator, but not after the last property
            if (i < displayProperties.Count - 1)
            {
                sb.Append(" - ");
            }
        }

        sb.Append("\";");

        return sb.ToString();
    }


}


