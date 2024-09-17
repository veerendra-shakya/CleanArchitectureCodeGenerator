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

        public TemplateMapper()
        {

        }

        public string GenerateClass(CSharpClassObject ModalClassObject, string targetFilePath, string targetProjectDirectory)
        {
            var relativePath = Utility.MakeRelativePath(ApplicationHelper.RootDirectory, Path.GetDirectoryName(targetFilePath) ?? "");

            string templateFile = Utility.GetTemplateFile(relativePath, targetFilePath);

            var template = ReplaceTokens(ModalClassObject, relativePath, templateFile, targetProjectDirectory);
            return Utility.NormalizeLineEndings(template);
        }



        private string ReplaceTokens(CSharpClassObject ModalClassObject, string relativePath, string templateFile, string targetProjectDirectory)
        {

            //using CleanArchitecture.Blazor.Application.Features.Customers.DTOs;
            if (string.IsNullOrEmpty(templateFile))
            {
                return templateFile;
            }

            var ns = ApplicationHelper.RootNamespace;
            if (!string.IsNullOrEmpty(relativePath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(relativePath);
            }
            ns = ns.TrimEnd('.');

            SnippetsWriter snippetsWriter = new SnippetsWriter();
            var nameofPlural = Utility.Pluralize(ModalClassObject.Name);
            var dtoFieldDefinition = snippetsWriter.CreateDtoFieldDefinition(ModalClassObject);
            var importFuncExpression = snippetsWriter.CreateImportFuncExpression(ModalClassObject);
            var templateFieldDefinition = snippetsWriter.CreateTemplateFieldDefinition(ModalClassObject);
            var exportFuncExpression = snippetsWriter.CreateExportFuncExpression(ModalClassObject);
            var mudTdDefinition = snippetsWriter.CreateMudTdDefinition(ModalClassObject);
            var mudTdHeaderDefinition = snippetsWriter.CreateMudTdHeaderDefinition(ModalClassObject);
            var mudFormFieldDefinition = snippetsWriter.CreateMudFormFieldDefinition(ModalClassObject);
            var fieldAssignmentDefinition = snippetsWriter.CreateFieldAssignmentDefinition(ModalClassObject);
            var fluentValidation = FluentValidationGenerator.GenerateFluentValidation(ModalClassObject);
            var masterProperty = ModalClassObject.Properties.Where(p => p.ScaffoldingAtt.PropRole == "Identifier").Select(p => p.PropertyName).FirstOrDefault();
            var efConfigurations = Ef_FluentConfigurationsGenerator.GenerateConfigurations(ModalClassObject);
            var advancedSpecificationQuery = snippetsWriter.CreateAdvancedSpecificationQuery(ModalClassObject);

            // Read the template file with UTF-8 encoding
            string content = string.Empty;

            content = File.ReadAllText(templateFile, Encoding.UTF8);

            // Replace tokens in the content
            content = content.Replace("{rootnamespace}", ApplicationHelper.RootNamespace);
            content = content.Replace("{domainprojectname}", ApplicationHelper.DomainProjectName);
            content = content.Replace("{uiprojectname}", ApplicationHelper.UiProjectName);
            content = content.Replace("{infrastructureprojectname}", ApplicationHelper.InfrastructureProjectName);
            content = content.Replace("{applicationprojectname}", ApplicationHelper.ApplicationProjectName);
            
            content = content.Replace("{selectns}", $"{ApplicationHelper.RootNamespace}.{Utility.GetProjectNameFromPath(targetProjectDirectory)}.Features");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", ModalClassObject.Name);
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
            content = content.Replace("{advancedSpecificationQuery}", advancedSpecificationQuery);
            content = content.Replace("{masterProperty}", masterProperty);
            content = content.Replace("{efConfigurations}", efConfigurations);

            return content;
        }
    }
}