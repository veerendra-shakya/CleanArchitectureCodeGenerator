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
        internal List<string> _templateFiles { get; set; } = new List<string>();
        internal string _defaultExt = ".txt";
        internal const string PRIMARYKEY = "Id";

        private string RootDirectory { get; set; }
        private string RootNamespace { get; set; }
        private string DomainProject { get; set; }
        private string UiProject { get; set; }
        private string InfrastructureProject { get; set; }
        private string ApplicationProject { get; set; }


        public TemplateMapper()
        {
            var assembly = Assembly.GetExecutingAssembly().Location;
            var _template_folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
            _templateFiles.AddRange(Directory.GetFiles(_template_folder, "*" + _defaultExt, SearchOption.AllDirectories));


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

            string templateFile = GetTemplateFile(relativePath, FileFullName);

            var template = ReplaceTokens(ModalClassObject, ModalClassName, relativePath, templateFile, TargetProjectDirectory);
            return Utility.NormalizeLineEndings(template);
        }

        private string GetTemplateFile(string relative, string file)
        {
            var list = _templateFiles.ToList();
            var templateFolders = new[]
            {
                "Commands\\AcceptChanges",
                "Commands\\Create",
                "Commands\\Delete",
                "Commands\\Update",
                "Commands\\AddEdit",
                "Commands\\Import",
                "DTOs",
                "Caching",
                "EventHandlers",
                "Events",
                "Specification",
                "Queries\\Export",
                "Queries\\GetAll",
                "Queries\\GetById",
                "Queries\\Pagination",
                "Pages",
                "Pages\\Components",
                "Persistence\\Configurations",
                "PermissionSet",
            };

            var extension = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);
            var safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);

            // Determine the folder pattern based on the relative path
            var folderPattern = templateFolders
                .FirstOrDefault(x => relative.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)
                ?.Replace("\\", "\\\\");

            if (!string.IsNullOrEmpty(folderPattern))
            {
                // Look for direct file name matches in the specified template folder
                var matchingFile = list
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault(f => Regex.IsMatch(f, folderPattern, RegexOptions.IgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(f).Split('.')
                                         .All(x => name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0));

                if (!string.IsNullOrEmpty(matchingFile))
                {
                    return matchingFile;
                }
            }

            // If no direct match, look for file extension matches
            var extensionMatch = list
                .FirstOrDefault(f => Path.GetFileName(f).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase) &&
                                     File.Exists(f));

            if (extensionMatch != null)
            {
                var adjustedName = AdjustForSpecific(safeName, extension);
                return Path.Combine(Path.GetDirectoryName(extensionMatch), adjustedName + _defaultExt);
            }

            // If no match is found, return null or throw an exception as per your requirement
            return null;
        }

        private string AdjustForSpecific(string safeName, string extension)
        {
            if (Regex.IsMatch(safeName, "^I[A-Z].*"))
            {
                return extension += "-interface";
            }

            return extension;
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


            return content;
        }
    }
}