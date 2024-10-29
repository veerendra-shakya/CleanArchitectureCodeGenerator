using CleanArchitecture.CodeGenerator.CodeWriter;
using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.ScribanCoder.Infrastructure.Persistence.Configurations
{
    public static class Configuration
    {

        public static void GenerateAll(bool force = false)
        {
            List<CSharpClassObject> KnownModelsList = new List<CSharpClassObject>();
            string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };
            KnownModelsList = ApplicationHelper.ClassObjectList?
            .Where(x => x.BaseClassNames != null && includes != null && includes.Any(baseName => x.BaseClassNames.Contains(baseName)) && !includes.Contains(x.Name))
            .ToList();

            foreach (var model in KnownModelsList)
            {
                Console.WriteLine($"{model.Name}");

                Generate(model, $"Persistence/Configurations/{model.Name}Configuration.cs", ApplicationHelper.InfrastructureProjectDirectory,true);
                //check if any property having many to many relationship then create linking table configurations
                Ef_RelationshipConfigurationsGenerator.GenerateConfigurations(model);

                Update_DbContext dbContextModifier = new Update_DbContext();
                var paths = dbContextModifier.SearchDbContextFiles(ApplicationHelper.RootDirectory);
                dbContextModifier.AddEntityProperty(paths, model.Name);

                Console.WriteLine($"\n");
            }
        }

        public static void Generate(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory, bool force = false)
        {
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
                string efConfigurations = Ef_FluentConfigurationsGenerator.GenerateConfigurations(modalClassObject);

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
                    efconfigurations = efConfigurations,
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

    }
}
