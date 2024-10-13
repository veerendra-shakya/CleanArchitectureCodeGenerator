using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Application.Features.Commands.AddEdit
{
    public static class AddEditCommandValidator
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
                string fluentValidation = FluentValidationGenerator.GenerateFluentValidation(modalClassObject);

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
                    fluentvalidation = fluentValidation,
                };

                // Parse and render the class template
                var classTemplate = Template.Parse(templateContent);
                string generatedClass = classTemplate.Render(masterdata);

                if (!string.IsNullOrEmpty(generatedClass))
                {
                    Utility.WriteToDiskAsync(targetFile.FullName, generatedClass);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Created file: {targetFile.FullName}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error generating file '{targetFile.FullName}': {ex.Message}");
                Console.ResetColor();
            }
        }

    }
}
