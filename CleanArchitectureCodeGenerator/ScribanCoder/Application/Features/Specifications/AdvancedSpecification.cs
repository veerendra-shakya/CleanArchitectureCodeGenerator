using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Specifications
{
    public static class AdvancedSpecification
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
                string advancedSpecificationQuery = CreateAdvancedSpecificationQuery(modalClassObject);

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
                    advancedspecificationquery = advancedSpecificationQuery,
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

        private static string CreateAdvancedSpecificationQuery(CSharpClassObject classObject)
        {
            // Get the item name from classObject.Name
            var itemName = classObject.Name;

            // Get the master property for identifier role
            var masterProperty = classObject.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();
            // Get the list of searchable properties
            var searchableProperty = classObject.ClassProperties.Where(p => p.ScaffoldingAtt.PropRole == "Searchable").Select(p => p.PropertyName).ToList();
            if (masterProperty != null)
            {
                searchableProperty.Add(masterProperty);
            }
            var output = new StringBuilder();

            // Start building the query with {itemname} replaced
            output.AppendLine($"Query.Where(q => q.{masterProperty} != null)");

            // Build the search condition for searchable properties
            if (searchableProperty.Any())
            {
                output.Append("         .Where(q => ");
                for (int i = 0; i < searchableProperty.Count; i++)
                {
                    output.Append($"q.{searchableProperty[i]}!.Contains(filter.Keyword)");

                    if (i < searchableProperty.Count - 1)
                    {
                        output.Append(" || ");
                    }
                }
                output.AppendLine(", !string.IsNullOrEmpty(filter.Keyword))");
            }

            // Add the rest of the conditions with {itemname} replaced
            output.AppendLine($"            .Where(q => q.CreatedBy == filter.CurrentUser.UserId, filter.ListView == {itemName}ListView.My && filter.CurrentUser is not null)");
            output.AppendLine($"            .Where(q => q.Created >= start && q.Created <= end, filter.ListView == {itemName}ListView.CreatedToday)");
            output.AppendLine($"            .Where(q => q.Created >= last30day, filter.ListView == {itemName}ListView.Created30Days);");

            // Return the built query as a string
            return output.ToString();
        }

    }
}
