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
    public class DemoEntityScaffolder
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

      
        public DemoEntityScaffolder()
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
        public void AddMasterDemoEntity()
        {
            string Target = $"Entities\\{"Master"}Demo.cs";
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
            content = content.Replace("{selectns}", $"{_rootNamespace}.{Utility.GetProjectNameFromPath(_domainProjectDir)}.Common.Entities");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", "MasterDemo");
            Utility.WriteToDiskAsync(TargerFilePath, content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=============================================================");
            Console.WriteLine("                    ADDED MASTER MODEL                       ");
            Console.WriteLine("=============================================================");
            Console.WriteLine($"Created file: {TargerFilePath}");
            Console.WriteLine("=============================================================");
            Console.ResetColor();
        }

        public void AddArticleDemoEntity()
        {
            string Target = $"Entities\\{"Demo"}Author.cs";
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
            content = content.Replace("{itemname}", "DemoAuthor");
            Utility.WriteToDiskAsync(TargerFilePath, content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=============================================================");
            Console.WriteLine("                    ADDED AUTHOR MODEL                       ");
            Console.WriteLine("=============================================================");
            Console.WriteLine($"Created file: {TargerFilePath}");
            Console.WriteLine("=============================================================");
            Console.ResetColor();
        }

    }
}
