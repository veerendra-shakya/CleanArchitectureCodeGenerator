using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder;

public static class AutocompleteRazorComponent
{
    public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory)
    {
        if (!Utility.ValidatePath(relativeTargetPath, targetProjectDirectory))
        {
            return;
        }

        FileInfo targetFile = new FileInfo(Path.Combine(targetProjectDirectory, relativeTargetPath));

        if (targetFile.Exists)
        {
            Console.WriteLine($"The file '{targetFile.FullName}' already exists.");
            return;
        }

        try
        {
            var relativePath = Utility.MakeRelativePath(ApplicationHelper.RootDirectory, Path.GetDirectoryName(targetFile.FullName) ?? "");
            string templateFilePath = Utility.GetTemplateFile(relativePath, targetFile.FullName);
            string templateContent = File.ReadAllText(templateFilePath, Encoding.UTF8);

            var NamespaceName = ApplicationHelper.RootNamespace;
            if (!string.IsNullOrEmpty(relativePath))
            {
                NamespaceName += "." + Utility.RelativePath_To_Namespace(relativePath);
            }
            NamespaceName = NamespaceName.TrimEnd('.');

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
                modelnameplural = modalClassObject.Name.Pluralize(),
                modelname = modalClassObject.Name,
                querystring = querystring,
                returnstring = returnstring,
            };

            // Parse and render the class template
            var classTemplate = Template.Parse(templateContent);
            string generatedClass = classTemplate.Render(masterdata);

            if (!string.IsNullOrEmpty(generatedClass))
            {
                Utility.WriteToDiskAsync(targetFile.FullName, generatedClass);
                Console.WriteLine($"Created file: {targetFile.FullName}");
            }


        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error generating file '{targetFile.FullName}': {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string ComposeQueryString(CSharpClassObject model)
    {
        var masterProperty = model.Properties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
        var displayProperties = model.Properties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

        if (masterProperty != null)
        {
            // Insert masterProperty at the beginning of the list
            displayProperties.Insert(0, masterProperty);
        }

        var sb = new StringBuilder();

        // Start composing the query string dynamically
        sb.Append($"result = _{model.Name}List")
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
          .Append("    .Select(x => x.Id as int?).ToList();");

        return sb.ToString();
    }

    private static string ComposeReturnString(CSharpClassObject model)
    {
        var masterProperty = model.Properties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
        var displayProperties = model.Properties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

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


