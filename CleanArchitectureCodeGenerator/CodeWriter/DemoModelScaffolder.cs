using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    public class DemoModelScaffolder
    {
        private readonly string _rootDirectory;
        private readonly string _rootNamespace;
        private readonly string _domainProject;
        private readonly string _uiProject;
        private readonly string _infrastructureProject;
        private readonly string _applicationProject;

        private readonly string _domainProjectDir;
        private readonly string _infrastructureProjectDir;
        private readonly string _uiProjectDir;
        private readonly string _applicationProjectDir;

      
        public DemoModelScaffolder()
        {
            var configHandler = new ConfigurationHandler("appsettings.json");
            var configSettings = configHandler.GetConfiguration();

            _rootDirectory = configSettings.RootDirectory;
            _rootNamespace = configSettings.RootNamespace;
            _domainProject = configSettings.DomainProject;
            _uiProject = configSettings.UiProject;
            _infrastructureProject = configSettings.InfrastructureProject;
            _applicationProject = configSettings.ApplicationProject;

            _domainProjectDir = Path.Combine(_rootDirectory, _domainProject);
            _infrastructureProjectDir = Path.Combine(_rootDirectory, _infrastructureProject);
            _uiProjectDir = Path.Combine(_rootDirectory, _uiProject);
            _applicationProjectDir = Path.Combine(_rootDirectory, _applicationProject);

        }
        
        public void AddSupportedDataTypesDemoEntity()
        {
            AddDemoEntity("DemoDataType", "Entities\\DemoDataType.cs");
        }
        
        public void AddValidationsDemoEntity()
        {
            AddDemoEntity("DemoValidation", "Entities\\DemoValidation.cs");
        }

        public void AddRelationshipDemoEntity()
        {
            AddDemoEntity("DemoStudent", "Entities\\DemoStudent.cs");
            AddDemoEntity("DemoProfile", "Entities\\DemoProfile.cs");
            AddDemoEntity("DemoSchool", "Entities\\DemoSchool.cs");
            AddDemoEntity("DemoCourse", "Entities\\DemoCourse.cs");
            AddDemoEntity("DemoStudentCourse", "Entities\\DemoStudentCourse.cs");
        }


        private void AddDemoEntity(string entityName, string relativePath)
        {
            string targetFilePath = Path.Combine(_domainProjectDir, relativePath);
            var relativeDirectoryPath = Utility.MakeRelativePath(_rootDirectory, Path.GetDirectoryName(targetFilePath) ?? "");
            string templateFilePath = Utility.GetTemplateFile(relativeDirectoryPath, targetFilePath);
            string content = File.ReadAllText(templateFilePath, Encoding.UTF8);

            var ns = _rootNamespace;
            if (!string.IsNullOrEmpty(relativeDirectoryPath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(relativeDirectoryPath);
            }
            ns = ns.TrimEnd('.');

            // Replace tokens in the content
            content = content.Replace("{rootnamespace}", _rootNamespace);
            content = content.Replace("{selectns}", $"{_rootNamespace}.{Utility.GetProjectNameFromPath(_domainProjectDir)}");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", entityName);

            Utility.WriteToDiskAsync(targetFilePath, content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=============================================================");
            Console.WriteLine($"Created file: {targetFilePath}");
            Console.WriteLine("=============================================================\n");
            Console.ResetColor();
        }

        private void AddDemoEntity(string EntityName)
        {
            string Target = $"Entities\\{EntityName}.cs";
            string TargerFilePath = Path.Combine(_domainProjectDir, Target);
            var RelativePath = Utility.MakeRelativePath(_rootDirectory, Path.GetDirectoryName(TargerFilePath) ?? "");
            string TemplateFilePath = Utility.GetTemplateFile(RelativePath, TargerFilePath);
            string content = File.ReadAllText(TemplateFilePath, Encoding.UTF8);

            var ns = _rootNamespace;
            if (!string.IsNullOrEmpty(RelativePath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(RelativePath);
            }
            ns = ns.TrimEnd('.');

            // Replace tokens in the content
            content = content.Replace("{rootnamespace}", _rootNamespace);
            content = content.Replace("{selectns}", $"{_rootNamespace}.{Utility.GetProjectNameFromPath(_domainProjectDir)}");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", EntityName);
            Utility.WriteToDiskAsync(TargerFilePath, content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n");
            Console.WriteLine("=============================================================");
            Console.WriteLine($"Created file: {TargerFilePath}");
            Console.WriteLine("=============================================================");
            Console.WriteLine("\n");
            Console.ResetColor();
        }

    }
}
