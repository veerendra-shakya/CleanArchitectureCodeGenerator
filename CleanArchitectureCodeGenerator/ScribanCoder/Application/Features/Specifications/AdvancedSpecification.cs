using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Specifications
{
    public static class AdvancedSpecification
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
                    modelnameplural = modalClassObject.NamePlural,
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
            var itemName = classObject.Name;

            var masterProperty = classObject.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();
            var searchableProperty = classObject.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Searchable").Select(p => p.PropertyName).ToList();
            var foreignKeyProperties = classObject.ClassProperties.Where(p => p.DataUsesAtt.IsForeignKey).ToList();

            if (masterProperty != null)
            {
                searchableProperty.Add(masterProperty);
            }
            var output = new StringBuilder();

            output.AppendLine($"Query");
           // output.AppendLine($".Where(q => q.{masterProperty} != null)");

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

            if(foreignKeyProperties != null)
            {
                foreach (var _property in foreignKeyProperties)
                {
                    string Id = _property.PropertyName;
                    output.AppendLine($".Where(q => q.{Id} == filter.{Id}, filter.{Id} != Guid.Empty)");
                }
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
