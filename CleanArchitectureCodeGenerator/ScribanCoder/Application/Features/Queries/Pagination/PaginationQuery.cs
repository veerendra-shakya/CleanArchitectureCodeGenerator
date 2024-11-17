using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Queries.Pagination
{
    public static class PaginationQuery
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
                string StringCacheKey = GenerateStringCacheKey(modalClassObject);

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
                    stringcachekey = StringCacheKey,
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

        public static string GenerateStringCacheKey(CSharpClassObject model)
        {
            var identifierProperty = model.ClassProperties.FirstOrDefault(p => p.DataUsesAtt.PrimaryRole == "Identifier");
            var searchableProperties = model.ClassProperties.Where(p => p.DataUsesAtt.PrimaryRole == "Searchable").ToList();
            var foreignKeyProperties = model.ClassProperties.Where(p => p.DataUsesAtt.IsForeignKey).ToList();
            
            if (identifierProperty != null)
            {
                searchableProperties.Insert(0, identifierProperty);
            }
            
            if (foreignKeyProperties != null)
            {
                searchableProperties.AddRange(foreignKeyProperties);
            }



            var output = new StringBuilder();

            output.Append("CurrentUser:{CurrentUser?.UserId}");
            output.Append(",ListView:{ListView}");
            output.Append(",Search:{Keyword}");
            output.Append(",SortDirection:{SortDirection}");
            output.Append(",OrderBy:{OrderBy},{PageNumber},{PageSize}");
            
            foreach (var property in searchableProperties)
            {
                output.Append($",{property.PropertyName}:{{{property.PropertyName}}}");
            }

            return output.ToString();
        }
    }
}
