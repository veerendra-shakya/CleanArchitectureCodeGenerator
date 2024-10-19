using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System.Reflection;
using System.Text;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.UI.Components.Dialogs.MultiSelector
{
    public static class MultipleSelectorDialog
    {
        public static void Generate(CSharpClassObject modal, string relativeTargetPath, string targetProjectDirectory)
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

                var masterProperty = modal.ClassProperties.FirstOrDefault(p => p.ScaffoldingAtt.PropRole == "Identifier");

                string identifierproperty = masterProperty.PropertyName;

                string FilteredItemsQuery = CreateFilteredItemsQuery(modal);
                string MudDataGridPropertyColumns = CreateMudDataGridPropertyColumns(modal);
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
                    modelnameplural = modal.Name.Pluralize(),
                    modelname = modal.Name,
            
                    identifierproperty,
                    filtereditemsquery = FilteredItemsQuery,
                    muddatagridpropertycolumns = MudDataGridPropertyColumns,
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

        private static string CreateMudDataGridPropertyColumns(CSharpClassObject classObject)
        {
            var output = new StringBuilder();

            // Loop through class properties and create PropertyColumn for each
            foreach (var prop in classObject.ClassProperties.Where(x=>x.Type.IsKnownType))
            {
                // Compose the PropertyColumn code using property name and title
                output.AppendLine($"<PropertyColumn Property=\"x => x.{prop.PropertyName}\" Title=\"{prop.PropertyName}\">");
                output.AppendLine("    <CellTemplate>");
                output.AppendLine($"        <MudText>@context.Item.{prop.PropertyName}</MudText>");
                output.AppendLine("    </CellTemplate>");
                output.AppendLine("</PropertyColumn>");
                output.AppendLine();
            }

            // Return the built code as a string
            return output.ToString();
        }

        private static string CreateFilteredItemsQuery(CSharpClassObject classObject)
        {
            // Get the item name from classObject.Name
            var itemName = classObject.Name;

            // Get the master property for identifier role
            var masterProperty = classObject.ClassProperties
                .Where(p => p.ScaffoldingAtt.PropRole == "Identifier")
                .Select(p => p.PropertyName)
                .FirstOrDefault();

            // Get the list of searchable properties
            var searchableProperties = classObject.ClassProperties
                .Where(p => p.ScaffoldingAtt.PropRole == "Searchable")
                .Select(p => p.PropertyName)
                .ToList();

            // Add the master property to searchable list if not null
            if (masterProperty != null)
            {
                // Insert masterProperty at the beginning of the list
                searchableProperties.Insert(0, masterProperty);
            }

            var output = new StringBuilder();

            // Start building the query
            output.AppendLine("// First filter the items based on the search text");
            output.AppendLine("var filtered = string.IsNullOrEmpty(SearchText)");
            output.AppendLine("    ? AllItems");
            output.AppendLine("    : AllItems.Where(x =>");

            // Loop through searchable properties and add conditions
            for (int i = 0; i < searchableProperties.Count; i++)
            {
                var property = searchableProperties[i];

                // Append condition for string properties
                output.AppendLine($"        x.{property} != null && x.{property}.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase)");

                // Add "or" between conditions if not the last property
                if (i < searchableProperties.Count - 1)
                {
                    output.AppendLine("        ||");
                }
            }

            // Finish the query
            output.AppendLine("    ).ToList();");

            output.AppendLine();
            output.AppendLine("// Prioritize selected items to appear on top");
            output.AppendLine("var selectedItems = filtered.Where(x => SelectedItemsInternal.Any(s => s.Id == x.Id)).ToList();");
            output.AppendLine("var unselectedItems = filtered.Where(x => !SelectedItemsInternal.Any(s => s.Id == x.Id)).ToList();");
            output.AppendLine();
            output.AppendLine("// Combine selected and unselected items, showing selected items on top");
            output.AppendLine("FilteredItems = selectedItems.Concat(unselectedItems).ToList();");


            // Return the built query as a string
            return output.ToString();
        }

    }
}
