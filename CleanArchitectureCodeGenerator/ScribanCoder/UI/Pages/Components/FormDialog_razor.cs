using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Drawing;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.UI.Pages.Components;

public static class FormDialog_razor
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
                modelnameplural = modalClassObject.NamePlural,
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
            if (property.PropertyName == "Deleted") continue;
            if (property.PropertyName == "DeletedOn") continue;
            if (property.PropertyName == "DeletedBy") continue;

            if (HasAttribute(property, "DataEditor"))
            {
                output.Append(HandleDataEditorAttribute(property, classObject));
                continue;
            }

            if (property.Type.TypeName.Contains("Enum"))
            {
                output.Append(MudItem(GenerateEnumSelectComponent(property), 6));
                continue;
            }
            if (property.Type.IsList && property.Type.TypeName == "List<string>?")
            {
                output.Append(MudItem(GenerateListStringTextEditorComponent(property), 6));
                continue;
            }
            if (property.Type.TypeName.Contains("JsonImage") || property.Type.TypeName.Contains("JsonFile"))
            {
                output.Append(MudItem(GenerateUploadComponent(property,"Default"), 12));
                continue;
            }
            if (property.DataUsesAtt.Has && property.DataUsesAtt.PrimaryRole == "Relationship")
            {
                output.Append(MudItem(GenerateCustomRelationshipComponent(property, classObject), 12));
                continue;
            }

            output.Append(MudItem(GenerateDefaultComponent(property), 6));
               
        }
        return output.ToString();
    }

    private static string HandleDataEditorAttribute(ClassProperty property, CSharpClassObject model)
    {
        var output = new StringBuilder();
        
        var attribute = property.propertyDeclarationSyntax.AttributeLists
            .SelectMany(a => a.Attributes)
            .FirstOrDefault(a => a.Name.ToString().Contains("DataEditor"));

        if (attribute != null && attribute.ArgumentList?.Arguments.Count >= 1)
        {
            string directoryName = string.Empty;
            string enumName = string.Empty;
            string refProperty = string.Empty;
            int width = 6;
            bool lineBreak = false; 

            // Extract the main component type (first argument) without "EditorType." prefix
            var componentArg = attribute.ArgumentList.Arguments[0].ToString();
            if (componentArg.StartsWith("EditorType."))
            {
                componentArg = componentArg.Replace("EditorType.", string.Empty);
            }

            // Extract additional arguments based on their names or positions
            foreach (var argument in attribute.ArgumentList.Arguments.Skip(1)) // Skip the first, which is the component
            {
                var argString = argument.ToString();

                // Check if the argument is named and set the respective variable
                if (argString.Contains("directoryName:"))
                {
                    directoryName = argument.ToString().Replace("directoryName:", "").Trim().Trim('"');
                }
                else if (argString.Contains("enumName:"))
                {
                    enumName = argument.ToString().Replace("enumName:", "").Trim().Trim('"');
                }
                else if (argString.Contains("refProperty:"))
                {
                    refProperty = argument.ToString().Replace("refProperty:", "").Trim().Trim('"');
                }
                else if (argString.Contains("width:"))
                {
                    if (int.TryParse(argument.ToString().Replace("width:", "").Trim(), out int parsedWidth))
                    {
                        width = parsedWidth;
                    }
                }
                else if (argString.Contains("lineBreak:"))
                {
                    // Parse and assign to the lineBreak variable
                    bool.TryParse(argument.ToString().Replace("lineBreak:", "").Trim(), out lineBreak);
                }
            }

            // Generate the component output based on the component type
            switch (componentArg)
            {
                case "None":
                    output.Append("");
                    break;
                case "EnumMudSelect":
                    output.Append(MudItem(GenerateEnumSelectComponent(property), width, lineBreak));
                    break;
                case "SortOrder":
                    output.Append(MudItem(GenerateSortOrder(property, model.NamePlural), width, lineBreak));
                    break;
                case "ListStringTextEditor":
                    output.Append(MudItem(GenerateListStringTextEditorComponent(property), width, lineBreak));
                    break;
                case "ListStringCheckBoxMultiSelection":
                    output.Append(MudItem(GenerateListStringCheckBoxMultiSelection(property, enumName), width, lineBreak));
                    break;
                case "ListStringMudSelectMultiSelection":
                    output.Append(MudItem(GenerateListStringMudSelectMultiSelection(property, enumName), width, lineBreak));
                    break;
                case "DictionaryStringKeyValueEditor":
                    output.Append(MudItem(GenerateDictionaryStringKeyValueEditor(property), width, lineBreak));
                    break;
                case "HtmlEditor":
                    output.Append(MudItem(GenerateHtmlEditor(property), width, lineBreak));
                    break;
                case "SlugTextField":
                    output.Append(MudItem(GenerateSlugTextField(property, refProperty), width, lineBreak));
                    break;
                case "Upload":
                    output.Append(MudItem(GenerateUploadComponent(property, directoryName), width, lineBreak));
                    break;
                case "JsonEditor":
                    output.Append(MudItem(GenerateJsonEditor(property), width, lineBreak));
                    break;
                case "CustomComponent":
                    output.Append(MudItem(GenerateCustomComponent(property), width, lineBreak));
                    break;
                    

                case "TextField":
                    output.Append(MudItem(GenerateTextField(property), width, lineBreak));
                    break;
                case "MultilineTextField":
                    output.Append(MudItem(GenerateMultilineTextField(property), width, lineBreak));
                    break;
                case "NumericField":
                    output.Append(MudItem(GenerateNumericField(property), width, lineBreak));
                    break;
                case "Select":
                    output.Append(MudItem(GenerateSelect(property), width, lineBreak));
                    break;
                case "Checkbox":
                    output.Append(MudItem(GenerateCheckbox(property), width, lineBreak));
                    break;
                case "RadioGroup":
                    output.Append(MudItem(GenerateRadioGroup(property), width, lineBreak));
                    break;
                case "DatePicker":
                    output.Append(MudItem(GenerateDatePicker(property), width, lineBreak));
                    break;
                case "Switch":
                    output.Append(MudItem(GenerateSwitch(property), width, lineBreak));
                    break;
                case "Slider":
                    output.Append(MudItem(GenerateSlider(property), width, lineBreak));
                    break;
                default:
                    output.Append(MudItem(GenerateDefaultComponent(property), width, lineBreak));
                    break;
            }
        }

        return output.ToString();
    }

    #region Component Generators 
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
            // output.AppendLine($"<{component} For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Placeholder=\"Select...\"></{component}>");
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

    private static string GenerateCustomRelationshipComponent(ClassProperty property, CSharpClassObject model)
    {
        var output = new StringBuilder();
        if (property.DataUsesAtt.PrimaryRole == "Relationship")
        {
            if (property.DataUsesAtt.RelationshipType == "OneToOne")
            {
                if (property.DataUsesAtt.IsForeignKey)
                {
                    // Scaffold AutoComplete Component
                    string refPropName = property.PropertyName;
                    if (refPropName.EndsWith("Id", StringComparison.Ordinal))
                    {
                        refPropName = refPropName.Substring(0, refPropName.Length - 2);
                        ClassProperty? refProperty = model.ClassProperties.Where(x => x.PropertyName == refPropName).FirstOrDefault();
                        if (refProperty != null)
                        {
                            string CustomAutocompletePicker = $"{refProperty.Type.TypeName}Autocomplete";
                            output.AppendLine($"<{CustomAutocompletePicker} For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Placeholder=\"Select...\"></{CustomAutocompletePicker}>");
                        }
                    }
                }
            }
            if (property.DataUsesAtt.RelationshipType == "OneToMany")
            {

            }
            if (property.DataUsesAtt.RelationshipType == "ManyToOne")
            {
                if (property.DataUsesAtt.IsForeignKey)
                {
                    // Scaffold AutoComplete Component
                    string Id_PropName = property.PropertyName;
                    if (Id_PropName.EndsWith("Id", StringComparison.Ordinal))
                    {
                        Id_PropName = Id_PropName.Substring(0, Id_PropName.Length - 2);
                        ClassProperty? refProperty = model.ClassProperties.Where(x => x.PropertyName == Id_PropName).FirstOrDefault();
                        if (refProperty != null)
                        {
                            //  output.AppendLine(GenerateManyToOneSelectComponent(property,refProperty));

                            string CustomAutocompletePicker = $"{refProperty.Type.TypeName.Replace("?", "")}Autocomplete";
                            output.AppendLine($"<{CustomAutocompletePicker} For=\"@(() => model.{property.PropertyName})\" @bind-Value=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" Placeholder=\"Select...\"></{CustomAutocompletePicker}>");
                        }
                    }
                }
            }
            if (property.DataUsesAtt.RelationshipType == "ManyToMany")
            {
                string datatype = Helper.ExtractDataType(property.Type.TypeName);
                output.AppendLine($"<{datatype}MultiSelectorDialog @bind-SelectedItems=\"@model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\"></{datatype}MultiSelectorDialog>");

            }
        }
        return output.ToString();
    }

    private static string GenerateUploadComponent(ClassProperty property, string directoryName)
    {
        var output = new StringBuilder();

        if (property.Type.TypeName.Contains("List<JsonImage>?"))
        {
            output.AppendLine($"<UploadMultipleImages @bind-Images=\"model.{property.PropertyName}\" AccessPermission=\"AccessPermission.Public\" Label=\"Upload {property.DisplayName}\" DirectoryName=\"{directoryName}\"/>");
        }
        if (property.Type.TypeName.Contains("List<JsonFile>?"))
        {
            output.AppendLine($"<UploadMultipleFiles @bind-Files=\"model.{property.PropertyName}\" AccessPermission=\"AccessPermission.Public\" Label=\"Upload {property.DisplayName}\" DirectoryName=\"{directoryName}\"/>");
        }
        if (property.Type.TypeName.Contains("JsonImage?"))
        {
            output.AppendLine($"<UploadSingleImage @bind-Image=\"model.{property.PropertyName}\" AccessPermission=\"AccessPermission.Public\" Label=\"Upload {property.DisplayName}\" DirectoryName=\"{directoryName}\"/>");
        }
        if (property.Type.TypeName.Contains("JsonFile?"))
        {
            output.AppendLine($"<UploadSingleFile @bind-File=\"model.{property.PropertyName}\" AccessPermission=\"AccessPermission.Public\" Label=\"Upload {property.DisplayName}\" DirectoryName=\"{directoryName}\"/>");
        }

        return output.ToString();
    }

    private static string GenerateEnumSelectComponent(ClassProperty property)
    {
        var output = new StringBuilder();

        output.AppendLine($"<MudSelect @bind-Value=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\">");
        output.AppendLine($"    @foreach ({property.Type.TypeName.Replace("?", "")} item in Enum.GetValues(typeof({property.Type.TypeName.Replace("?", "")})))");
        output.AppendLine("    {");
        output.AppendLine("        <MudSelectItem Value=\"@item\">@item.GetDescription()</MudSelectItem>");
        output.AppendLine("    }");
        output.AppendLine("</MudSelect>");

        return output.ToString();
    }

    private static string GenerateListStringTextEditorComponent(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<ListStringTextEditor @bind-Items=\"model.{property.PropertyName}\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\"></ListStringTextEditor>");
        return output.ToString();
    }

    private static string GenerateListStringCheckBoxMultiSelection(ClassProperty property, string enumName)
    {
        var output = new StringBuilder();
        output.AppendLine($"<ListStringCheckBoxMultiSelection @bind-Items=\"model.{property.PropertyName}\" DataSource=\"typeof({enumName})\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\"></ListStringCheckBoxMultiSelection>");
        return output.ToString();
    }

    private static string GenerateListStringMudSelectMultiSelection(ClassProperty property, string enumName)
    {
        var output = new StringBuilder();
        output.AppendLine($"<ListStringMudSelectMultiSelection @bind-Items=\"model.{property.PropertyName}\" DataSource=\"typeof({enumName})\" Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\"></ListStringMudSelectMultiSelection>");
        return output.ToString();
    }

    private static string GenerateDictionaryStringKeyValueEditor(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<DictionaryStringKeyValueEditor Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Items=\"model.{property.PropertyName}\" />");
        return output.ToString();
    }

    private static string GenerateHtmlEditor(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<HtmlEditor Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\"></HtmlEditor>");
        return output.ToString();
    }

    private static string GenerateSlugTextField(ClassProperty property, string refProperty)
    {
        var output = new StringBuilder();
        output.AppendLine($"<SlugTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" @bind-InputText=\"model.{refProperty}\"></SlugTextField>");
        return output.ToString();
    }

    private static string GenerateSortOrder(ClassProperty property,string modelName)
    {
        var output = new StringBuilder();
        output.AppendLine($"<SortOrder Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" Query=\"new Get{modelName}CountQuery()\"></SortOrder>");
        return output.ToString();
    }

    private static string GenerateManyToOneSelectComponent(ClassProperty propertyId, ClassProperty refproperty)
    {
        var output = new StringBuilder();

        string refpropType = refproperty.Type.TypeName.Replace("?", "");
        CSharpClassObject? model = ApplicationHelper.ClassObjectList.Where(x => x.Name == refpropType).FirstOrDefault();
        if (model != null)
        {
            var masterProperty = model.ClassProperties.FirstOrDefault(p => p.DataUsesAtt.PrimaryRole == "Identifier");
            var displayProperties = model.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Searchable").ToList();

            if (masterProperty != null)
            {
                // Insert masterProperty at the beginning of the list
                displayProperties.Insert(0, masterProperty);
            }

            output.AppendLine($"<MudSelect For=\"@(() => model.{propertyId.PropertyName})\"");
            output.AppendLine($"           Label=\"@L[model.GetMemberDescription(x=>x.{propertyId.PropertyName})]\"");
            output.AppendLine($"           Required=\"true\"");
            output.AppendLine($"           RequiredError=\"@L[\"{propertyId.DisplayName} is required.\"]\"");
            output.AppendLine($"           @bind-Value=\"@model.{propertyId.PropertyName}\">");
            output.AppendLine($"    @foreach (var item in {refproperty.Type.TypeName.Replace("?", "")}Service.DataSource)");
            output.AppendLine("    {");
            output.AppendLine($"        <MudSelectItem T=\"{propertyId.Type.TypeName}\" Value=\"@item.Id\">@item.{masterProperty.PropertyName}</MudSelectItem>");
            output.AppendLine("    }");
            output.AppendLine("</MudSelect>");
        }

        return output.ToString();
    }

    private static string GenerateJsonEditor(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Lines=\"5\"></MudTextField>");
        return output.ToString();
    }

    private static string GenerateCustomComponent(ClassProperty property)
    {
        var output = new StringBuilder();
        string CustomComponentTag = property.Type.TypeName.Replace("?", "") + "Editor";
        output.AppendLine($"<{CustomComponentTag} Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Quota=\"model.{property.PropertyName}\"></{CustomComponentTag}>");
        return output.ToString();
    }

    private static string GenerateTextField(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"></MudTextField>");
        return output.ToString();
    }
    private static string GenerateMultilineTextField(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudTextField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Lines=\"5\"></MudTextField>");
        return output.ToString();
    }
    private static string GenerateNumericField(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudNumericField Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Min=\"0\"></MudNumericField>");
        return output.ToString();
    }
    private static string GenerateSelect(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"");
        return output.ToString();
    }
    private static string GenerateCheckbox(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudCheckBox Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Checked=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\"></MudCheckBox>");
        return output.ToString();
    }
    private static string GenerateRadioGroup(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"");
        return output.ToString();
    }
    private static string GenerateDatePicker(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudDatePicker Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Date=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Required=\"false\" RequiredError=\"@L[\"{Utility.SplitCamelCase(property.PropertyName).ToLower()} is required!\"]\"></MudDatePicker>");
        return output.ToString();
    }
    private static string GenerateSwitch(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"<MudSwitch Label=\"@L[model.GetMemberDescription(x=>x.{property.PropertyName})]\" @bind-Value=\"model.{property.PropertyName}\" For=\"@(() => model.{property.PropertyName})\" Color=\"Color.Primary\" UncheckedColor=\"Color.Secondary\" />");
        return output.ToString();
    }
    private static string GenerateSlider(ClassProperty property)
    {
        var output = new StringBuilder();
        output.AppendLine($"");
        return output.ToString();
    }

    #endregion

    #region Helper Functions
    private static bool HasAttribute(ClassProperty property, string attributeName)
    {
        var attributeLists = property.propertyDeclarationSyntax.AttributeLists;
        var attributes = attributeLists.SelectMany(a => a.Attributes);
        var attributeNames = attributes.Select(a => a.Name.ToString());
        var hasAttribute = attributeNames.Any(name => name.Contains(attributeName));
        return hasAttribute;
    }

    private static string MudItem(string componentDefinition, int size, bool lineBreak = false)
    {
        // Build the output with calculated sizes for xs, sm, and md
        var output = new StringBuilder();
        output.AppendLine($"<MudItem xs=\"12\" sm=\"{size}\" md=\"{size}\">");
        output.Append($"    {componentDefinition}");
        output.AppendLine("</MudItem>");
        if(lineBreak)
        {
            output.AppendLine("<MudFlexBreak />");
        }
        return output.ToString();
    }

    #endregion

}
