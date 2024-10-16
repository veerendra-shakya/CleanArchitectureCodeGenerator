using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.UI.Pages.Components;

public static class FormDialog_razor
{
    public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory)
    {
        FileInfo? targetFile = Helper.GetFileInfo(relativeTargetPath, targetProjectDirectory);
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

            string mudformfielddefinition = CreateMudFormFieldDefinition(modalClassObject);

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
                mudformfielddefinition,
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

    public static string CreateMudFormFieldDefinition(CSharpClassObject classObject)
    {

        var output = new StringBuilder();
        foreach (var property in classObject.ClassProperties.Where(x => x.Type.IsKnownType))
        {
            if (property.PropertyName == "Id") continue;
            switch (property.Type.TypeName.ToLower())
            {
                case "string" when property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase):
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"true\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                    output.AppendLine("</MudItem>");
                    break;
                case "string" when property.PropertyName.Equals("Description", StringComparison.OrdinalIgnoreCase):
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Lines=\"3\" For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\"></MudTextField>");
                    output.AppendLine("</MudItem>");
                    break;
                case "bool?":
                case "bool":
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudCheckBox Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Checked=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"></MudCheckBox>");
                    output.AppendLine("</MudItem>");
                    break;
                case "int?":
                case "int":
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                    output.AppendLine("</MudItem>");
                    break;
                case "decimal?":
                case "decimal":
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0.00m\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                    output.AppendLine("</MudItem>");
                    break;
                case "double?":
                case "double":
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0.00\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                    output.AppendLine("</MudItem>");
                    break;
                case "system.datetime?":
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudDatePicker Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Date=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudDatePicker>");
                    output.AppendLine("</MudItem>");
                    break;
                default:
                    output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
                    output.AppendLine($"    <MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                    output.AppendLine("</MudItem>");
                    break;
            }
        }
        return output.ToString();
    }

}
