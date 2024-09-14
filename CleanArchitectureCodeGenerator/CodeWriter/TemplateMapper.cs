using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    public class TemplateMapper
    {
      
        internal const string PRIMARYKEY = "Id";

        private string RootDirectory { get; set; }
        private string RootNamespace { get; set; }
        private string DomainProject { get; set; }
        private string UiProject { get; set; }
        private string InfrastructureProject { get; set; }
        private string ApplicationProject { get; set; }


        public TemplateMapper()
        {
            //get config
            string configFilePath = "appsettings.json";
            var configHandler = new ConfigurationHandler(configFilePath);
            var configSettings = configHandler.GetConfiguration();

            RootDirectory = configSettings.RootDirectory;
            RootNamespace = configSettings.RootNamespace;
            DomainProject = configSettings.DomainProject;
            UiProject = configSettings.UiProject;
            InfrastructureProject = configSettings.InfrastructureProject;
            ApplicationProject = configSettings.ApplicationProject;
        }

        public string GenerateClass(CSharpClassObject ModalClassObject, string FileFullName, string ModalClassName, string TargetProjectDirectory)
        {
            var relativePath = Utility.MakeRelativePath(RootDirectory, Path.GetDirectoryName(FileFullName) ?? "");

            string templateFile = Utility.GetTemplateFile(relativePath, FileFullName);

            var template = ReplaceTokens(ModalClassObject, ModalClassName, relativePath, templateFile, TargetProjectDirectory);
            return Utility.NormalizeLineEndings(template);
        }

       

        private string ReplaceTokens(CSharpClassObject ModalClassObject, string ModalClassName, string relativePath, string templateFile, string TargetProjectDirectory)
        {

            //using CleanArchitecture.Blazor.Application.Features.Customers.DTOs;
            if (string.IsNullOrEmpty(templateFile))
            {
                return templateFile;
            }

            var ns = RootNamespace;
            if (!string.IsNullOrEmpty(relativePath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(relativePath);
            }
            ns = ns.TrimEnd('.');

            SnippetsWriter snippetsWriter = new SnippetsWriter();
            var nameofPlural = Utility.Pluralize(ModalClassName);
            var dtoFieldDefinition = snippetsWriter.CreateDtoFieldDefinition(ModalClassObject);
            var importFuncExpression = snippetsWriter.CreateImportFuncExpression(ModalClassObject);
            var templateFieldDefinition = snippetsWriter.CreateTemplateFieldDefinition(ModalClassObject);
            var exportFuncExpression = snippetsWriter.CreateExportFuncExpression(ModalClassObject);
            var mudTdDefinition = snippetsWriter.CreateMudTdDefinition(ModalClassObject);
            var mudTdHeaderDefinition = snippetsWriter.CreateMudTdHeaderDefinition(ModalClassObject);
            var mudFormFieldDefinition = snippetsWriter.CreateMudFormFieldDefinition(ModalClassObject);
            var fieldAssignmentDefinition = snippetsWriter.CreateFieldAssignmentDefinition(ModalClassObject);
            var fluentValidation = FluentValidationGenerator.GenerateFluentValidation(ModalClassObject);
            var masterProperty = ModalClassObject.Properties.Where(p => p.PropRole== "Identifier").Select(p => p.PropertyName).FirstOrDefault();
            var searchableProperty = ModalClassObject.Properties.Where(p => p.PropRole == "Searchable").Select(p => p.PropertyName).FirstOrDefault();
            var efConfigurations = EfConfigurationsGenerator.GenerateConfigurations(ModalClassObject);
            

            // Read the template file with UTF-8 encoding
            string content = string.Empty;

            content = File.ReadAllText(templateFile, Encoding.UTF8);

            // Replace tokens in the content
            content = content.Replace("{rootnamespace}", RootNamespace);
            content = content.Replace("{selectns}", $"{RootNamespace}.{Utility.GetProjectNameFromPath(TargetProjectDirectory)}.Features");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", ModalClassName);
            content = content.Replace("{nameofPlural}", nameofPlural);
            content = content.Replace("{dtoFieldDefinition}", dtoFieldDefinition);
            content = content.Replace("{fieldAssignmentDefinition}", fieldAssignmentDefinition);
            content = content.Replace("{importFuncExpression}", importFuncExpression);
            content = content.Replace("{templateFieldDefinition}", templateFieldDefinition);
            content = content.Replace("{exportFuncExpression}", exportFuncExpression);
            content = content.Replace("{mudTdDefinition}", mudTdDefinition);
            content = content.Replace("{mudTdHeaderDefinition}", mudTdHeaderDefinition);
            content = content.Replace("{mudFormFieldDefinition}", mudFormFieldDefinition);
            content = content.Replace("{fluentValidation}", fluentValidation);
            content = content.Replace("{masterProperty}", masterProperty);
            content = content.Replace("{searchableProperty}", searchableProperty);
            content = content.Replace("{efConfigurations}", efConfigurations);

            return content;
        }
    }
}