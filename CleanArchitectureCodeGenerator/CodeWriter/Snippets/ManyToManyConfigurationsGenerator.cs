using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator.ScribanCoder.UI.Components.Dialogs.MultiSelector;
using System.Text;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class ManyToManyConfigurationsGenerator
    {
        public static void GenerateConfigurations(CSharpClassObject classObject)
        {
            foreach (var property in classObject.ClassProperties)
            {
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
                    {
                        AddLinkingEntityConfiguration(property);

                        AddToDatabaseContext(property.ScaffoldingAtt.LinkingTable);

                        MultipleSelectorDialog.Generate(classObject, property,
                            $"Components/Dialogs/MultiSelector/{property.PropertyName}MultipleSelectorDialog.razor",
                            ApplicationHelper.UiProjectDirectory);
                    }
                }
            }
        }

        private static void AddLinkingEntityConfiguration(ClassProperty property)
        {
            string EntityName = property.ScaffoldingAtt.LinkingTable;
            string key1 = $"{property.PropertyName.Singularize()}Id";
            string key2 = $"{property.ScaffoldingAtt.InverseProperty.Singularize()}Id";

            #region Init Variables
            var configHandler = new ConfigurationHandler("appsettings.json");
            var configSettings = configHandler.GetConfiguration();

            string _rootDirectory = configSettings.RootDirectory;
            string _rootNamespace = configSettings.RootNamespace;
            string _domainProject = configSettings.DomainProject;
            string _uiProject = configSettings.UiProject;
            string _infrastructureProject = configSettings.InfrastructureProject;
            string _applicationProject = configSettings.ApplicationProject;

            string _domainProjectDir = Path.Combine(_rootDirectory, _domainProject);
            string _infrastructureProjectDir = Path.Combine(_rootDirectory, _infrastructureProject);
            string _uiProjectDir = Path.Combine(_rootDirectory, _uiProject);
            string _applicationProjectDir = Path.Combine(_rootDirectory, _applicationProject);
            #endregion


            string Target = $"Persistence/Configurations/{EntityName}Configuration.cs";
            string TargerFilePath = Path.Combine(_infrastructureProjectDir, Target);

            // Check if the file already exists
            if (File.Exists(TargerFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n");
                Console.WriteLine("=============================================================");
                Console.WriteLine($"Many to Many Linking Table Configuration file already exists: {TargerFilePath}");
                Console.WriteLine("=============================================================");
                Console.WriteLine("\n");
                Console.ResetColor();
                return; // Exit the function if file exists
            }


            var RelativePath = Utility.MakeRelativePath(_rootDirectory, Path.GetDirectoryName(TargerFilePath) ?? "");
            string TemplateFilePath = Utility.GetTemplateFile(RelativePath, TargerFilePath);
            string content = File.ReadAllText(TemplateFilePath, Encoding.UTF8);

            var ns = _rootNamespace;
            if (!string.IsNullOrEmpty(RelativePath))
            {
                ns += "." + Utility.RelativePath_To_Namespace(RelativePath);
            }
            ns = ns.TrimEnd('.');

            // Build Configurations
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"builder.ToTable(\"{EntityName}\");");
            sb.AppendLine($"builder.HasKey(x => new {{x.{key1},x.{key2}}});");
            string efConfigurations = sb.ToString();


            // Replace tokens in the content
            content = content.Replace("{rootnamespace}", _rootNamespace);
            content = content.Replace("{selectns}", $"{_rootNamespace}.{Utility.GetProjectNameFromPath(_domainProjectDir)}");
            content = content.Replace("{namespace}", ns);
            content = content.Replace("{itemname}", EntityName);
            content = content.Replace("{efConfigurations}", efConfigurations);
            content = content.Replace("builder.Ignore(e => e.DomainEvents);", "");

            Utility.WriteToDiskAsync(TargerFilePath, content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n");
            Console.WriteLine("=============================================================");
            Console.WriteLine($"Created Manay to Many Linking Table Configuration file: {TargerFilePath}");
            Console.WriteLine("=============================================================");
            Console.WriteLine("\n");
            Console.ResetColor();

        }

        private static void AddToDatabaseContext(string LinkingEntityName)
        {
            Update_DbContext dbContextModifier = new Update_DbContext();
            var paths = dbContextModifier.SearchDbContextFiles(ApplicationHelper.RootDirectory);
            dbContextModifier.AddEntityProperty(paths, LinkingEntityName);
        }

        public static void RemoveConfigurations(CSharpClassObject classObject)
        {
            foreach (var property in classObject.ClassProperties)
            {
                if (property.ScaffoldingAtt.PropRole == "Relationship")
                {
                    if (property.ScaffoldingAtt.RelationshipType == "ManyToMany")
                    {
                        string EntityName = property.ScaffoldingAtt.LinkingTable;
                        var configHandler = new ConfigurationHandler("appsettings.json");
                        var configSettings = configHandler.GetConfiguration();

                        string _rootDirectory = configSettings.RootDirectory;
                        string _infrastructureProject = configSettings.InfrastructureProject;
                        string _infrastructureProjectDir = Path.Combine(_rootDirectory, _infrastructureProject);

                        string Target = $"Persistence/Configurations/{EntityName}Configuration.cs";
                        string TargerFilePath = Path.Combine(_infrastructureProjectDir, Target);

                        // Check if the file exists and delete it
                        if (File.Exists(TargerFilePath))
                        {
                            File.Delete(TargerFilePath);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n");
                            Console.WriteLine("=============================================================");
                            Console.WriteLine($"Deleted Many to Many Linking Table Configuration file: {TargerFilePath}");
                            Console.WriteLine("=============================================================");
                            Console.WriteLine("\n");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\n");
                            Console.WriteLine("=============================================================");
                            Console.WriteLine($"Configuration file not found: {TargerFilePath}");
                            Console.WriteLine("=============================================================");
                            Console.WriteLine("\n");
                            Console.ResetColor();
                        }
                    }
                }
            }
        }


    }
}
