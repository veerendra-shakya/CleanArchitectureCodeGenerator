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
        foreach (var property in classObject.ClassProperties)//.Where(x => x.Type.IsKnownType)
        {
            if (property.PropertyName == "Id") continue;

            if (property.UIDesignAtt.Has)
            {
                output.Append(CreateComponentWithMudItem(GenerateCustomUIComponent(property)));
            }
            else if (property.ScaffoldingAtt.Has && property.ScaffoldingAtt.PropRole == "Relationship")
            {
                output.Append(CreateComponentWithMudItem(GenerateCustomRelationshipComponent(property)));
            }
            else
            {
                output.Append(CreateComponentWithMudItem(GenerateDefaultComponent(property)));
            }
        }
        return output.ToString();
    }


    private static string CreateComponentWithMudItem(string componentDefinition)
    {
        var output = new StringBuilder();
        output.AppendLine("<MudItem xs=\"12\" md=\"6\">");
        output.Append($"    {componentDefinition}");
        output.AppendLine("</MudItem>");
        return output.ToString();
    }

    private static string GenerateCustomUIComponent(ClassProperty property)
    {
        var output = new StringBuilder();
        string CustomAutocompletePicker = $"{property.UIDesignAtt.RelateWith}Autocomplete";
        string component = property.UIDesignAtt.CompType switch
        {
            "TextField" => "MudTextField",
            "Select" => "MudSelect",
            "Checkbox" => "MudCheckBox",
            "RadioGroup" => "MudRadioGroup",
            "DatePicker" => "MudDatePicker",
            "Switch" => "MudSwitch",
            "Slider" => "MudSlider",
            "Autocomplete" => "MudAutocomplete",
            "NumericField" => "MudNumericField",
            "CustomAutocompletePicker" => CustomAutocompletePicker,
            _ => "MudTextField",
        };

        if (component == CustomAutocompletePicker)
        {
            output.AppendLine($"<{component} For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Placeholder=\"Select...\"></{component}>");
            return output.ToString();
        }

        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.Label))
        {
            output.AppendLine($"<{component} Label=\"{property.UIDesignAtt.Label}\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"");
        }
        else
        {
            output.AppendLine($"<{component} Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"");
        }

        if (property.UIDesignAtt.MaxLength.HasValue && property.UIDesignAtt.MaxLength.Value > 0)
            output.AppendLine($"    MaxLength=\"{property.UIDesignAtt.MaxLength}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.HelperText))
            output.AppendLine($"    HelperText=\"{property.UIDesignAtt.HelperText}\"");
        if (property.UIDesignAtt.Clearable.HasValue && property.UIDesignAtt.Clearable.Value)
            output.AppendLine($"    Clearable=\"true\"");
        if (property.UIDesignAtt.Disabled.HasValue && property.UIDesignAtt.Disabled.Value)
            output.AppendLine($"    Disabled=\"true\"");
        if (property.UIDesignAtt.ReadOnly.HasValue && property.UIDesignAtt.ReadOnly.Value)
            output.AppendLine($"    ReadOnly=\"true\"");
        if (property.UIDesignAtt.AutoGrow.HasValue && property.UIDesignAtt.AutoGrow.Value)
            output.AppendLine($"    AutoGrow=\"true\"");
        if (property.UIDesignAtt.Lines.HasValue && property.UIDesignAtt.Lines.Value > 0)
            output.AppendLine($"    Lines=\"{property.UIDesignAtt.Lines}\"");
        if (property.UIDesignAtt.Adornment != null)
            output.AppendLine($"    Adornment=\"{property.UIDesignAtt.Adornment}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.AdornmentIcon))
            output.AppendLine($"    AdornmentIcon=\"{property.UIDesignAtt.AdornmentIcon}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.AdornmentColor))
            output.AppendLine($"    AdornmentColor=\"{property.UIDesignAtt.AdornmentColor}\"");
        if (property.UIDesignAtt.Counter.HasValue && property.UIDesignAtt.Counter.Value > 0)
            output.AppendLine($"    Counter=\"{property.UIDesignAtt.Counter}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.DataModel))
            output.AppendLine($"    DataModel=\"{property.UIDesignAtt.DataModel}\"");
        if (property.UIDesignAtt.HelperTextOnFocus.HasValue && property.UIDesignAtt.HelperTextOnFocus.Value)
            output.AppendLine($"    HelperTextOnFocus=\"true\"");
        if (property.UIDesignAtt.Immediate.HasValue && property.UIDesignAtt.Immediate.Value)
            output.AppendLine($"    Immediate=\"true\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.InputType))
            output.AppendLine($"    InputType=\"{property.UIDesignAtt.InputType}\"");
        if (property.UIDesignAtt.ShrinkLabel.HasValue && property.UIDesignAtt.ShrinkLabel.Value)
            output.AppendLine($"    ShrinkLabel=\"true\"");
        if (property.UIDesignAtt.MaxLines.HasValue && property.UIDesignAtt.MaxLines.Value > 0)
            output.AppendLine($"    MaxLines=\"{property.UIDesignAtt.MaxLines}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.Margin))
            output.AppendLine($"    Margin=\"{property.UIDesignAtt.Margin}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.Mask))
            output.AppendLine($"    Mask=\"{property.UIDesignAtt.Mask}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.Typography))
            output.AppendLine($"    Typography=\"{property.UIDesignAtt.Typography}\"");
        if (!string.IsNullOrWhiteSpace(property.UIDesignAtt.Variant))
            output.AppendLine($"    Variant=\"{property.UIDesignAtt.Variant}\"");

        output.AppendLine($"></{component}>");
        return output.ToString();
    }

    private static string GenerateDefaultComponent(ClassProperty property)
    {
        var output = new StringBuilder();

        switch (property.Type.TypeName.ToLower())
        {
            case "string" when property.PropertyName.Equals("Name", StringComparison.OrdinalIgnoreCase):
                output.AppendLine($"<MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"true\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                break;
            case "bool?":
            case "bool":
                output.AppendLine($"<MudCheckBox Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Checked=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"></MudCheckBox>");
                break;
            case "int?":
            case "int":
                output.AppendLine($"<MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudNumericField>");
                break;
            case "system.datetime?":
                output.AppendLine($"<MudDatePicker Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Date=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudDatePicker>");
                break;
            default:
                output.AppendLine($"<MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudTextField>");
                break;
        }
        return output.ToString();
    }

    private static string GenerateCustomRelationshipComponent(ClassProperty property)
    {
        var output = new StringBuilder();
        if (property.ScaffoldingAtt.PropRole == "Relationship")
        {
            if (property.ScaffoldingAtt.RelationshipType == "OneToOne")
            {

            }
            if (property.ScaffoldingAtt.RelationshipType == "OneToMany")
            {

            }
            if (property.ScaffoldingAtt.RelationshipType == "ManyToOne")
            {

            }
            if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
            {
                string datatype = Helper.ExtractDataType(property.Type.TypeName);
                output.AppendLine($"<{datatype}MultiSelectorDialog @bind-SelectedItems=\"@model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\"></{datatype}MultiSelectorDialog>");

            }
        }
        return output.ToString();
    }

}
