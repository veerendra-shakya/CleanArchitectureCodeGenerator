using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder;

public static class API_Controller
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
            string NamespaceName = Utility.GetNameSpace(relativePath);

            string codeofgetfunction = ComposeGetFunction(modalClassObject);
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
               // modelnameplurallower = modalClassObject.Name.Pluralize().ToLower(),
                modelname = modalClassObject.Name,
                modelnamelower = modalClassObject.Name.ToLower(),
                codeofgetfunction = codeofgetfunction,
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

  

    private static string ComposeGetFunction(CSharpClassObject model)
    {
        var masterProperty = model.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");
        var searchableProperties = model.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").ToList();

        if (masterProperty != null)
        {
            // Insert masterProperty at the beginning of the list
            searchableProperties.Insert(0, masterProperty);
        }
   
        var sb = new StringBuilder();

        // Start the method definition
        sb.AppendLine($"public async Task<IActionResult> Get{model.Name}(");

        // Generate [FromQuery] parameters
        for (int i = 0; i < searchableProperties.Count; i++)
        {
            var prop = searchableProperties[i];
            sb.AppendLine($"       [FromQuery] {prop.Type.TypeName} {prop.PropertyName.ToLower()},");
        }
        // Add pagination parameters
        sb.AppendLine("       [FromQuery] int pageNumber = 1,");
        sb.AppendLine("       [FromQuery] int pageSize = 10)");

        // Begin method body
        sb.AppendLine("    {");
        sb.AppendLine($"        var query = new {model.Name.Pluralize()}WithPaginationQuery");
        sb.AppendLine("        {");

        // Assign parameters to the query object
        foreach (var prop in searchableProperties)
        {
            sb.AppendLine($"            {prop.PropertyName} = {prop.PropertyName.ToLower()},");
        }

        // Add pagination parameters
        sb.AppendLine("            PageNumber = pageNumber,");
        sb.AppendLine("            PageSize = pageSize");
        sb.AppendLine("        };");

        // Send query to the mediator
        sb.AppendLine("        var result = await _mediator.Send(query);");
        sb.AppendLine("        if (result != null && result.Items.Any())");
        sb.AppendLine("        {");
        sb.AppendLine($"            return Ok(result.Items);");
        sb.AppendLine("        }");

        // Return not found if no results
        sb.AppendLine($"        return NotFound(new {{ Message = \"No {model.Name.ToLower()} found.\" }});");
        sb.AppendLine("    }");

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


