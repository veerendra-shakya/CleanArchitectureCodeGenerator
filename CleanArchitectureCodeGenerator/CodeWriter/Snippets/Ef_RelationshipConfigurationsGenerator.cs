﻿using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator.ScribanCoder;
using CleanArchitecture.CodeGenerator.ScribanCoder.UI.Components.Dialogs.MultiSelector;
using CleanArchitecture.CodeGenerator.ScribanCoder.UI.Components.Dialogs.SingleSelector;
using Humanizer;
using Scriban;
using System.Text;

namespace CleanArchitecture.CodeGenerator.CodeWriter.Snippets
{
    public static class Ef_RelationshipConfigurationsGenerator
    {
        public static void GenerateConfigurations(CSharpClassObject classObject)
        {
            foreach (var property in classObject.ClassProperties)
            {
                if (property.DataUsesAtt.PrimaryRole == "Relationship")
                {
                    if (property.DataUsesAtt.RelationshipType == "OneToOne")
                    {
                        string _rObject = property.Type.TypeName;
                        CSharpClassObject? RelatedTableObject = ApplicationHelper.ClassObjectList.Where(x => x.Name == _rObject).FirstOrDefault();
                        if (RelatedTableObject != null)
                        {
                            SingleSelectorDialog.Generate(RelatedTableObject,
                               $"Components/Dialogs/SingleSelector/{RelatedTableObject.Name}SingleSelectorDialog.razor",
                               ApplicationHelper.UiProjectDirectory);
                        }
                    }
                    if (property.DataUsesAtt.RelationshipType == "OneToMany")
                    {
                        // We are generating Autocompletes for all tables for code reusability.
                    }
                    if (property.DataUsesAtt.RelationshipType == "ManyToOne")
                    {
                       // We are generating Autocompletes for all tables for code reusability.
                    }
                    if (property.DataUsesAtt.RelationshipType == "ManyToMany")
                    {
                        CSharpClassObject? RelatedTableObject = GetRelatedTableObject(property);
                        GenerateLinkingEntityConfiguration(property);
                        AddToDatabaseContext(property.DataUsesAtt.LinkingTable);
                        if(RelatedTableObject != null)
                        {
                            MultipleSelectorDialog.Generate(RelatedTableObject, 
                               $"Components/Dialogs/MultiSelector/{RelatedTableObject.Name}MultiSelectorDialog.razor",
                               ApplicationHelper.UiProjectDirectory);
                        }
                    }
                }
            }
        }

        private static CSharpClassObject? GetRelatedTableObject(ClassProperty property)
        {
            string propertytype = property.Type.TypeName;
            string propertydatatype = Helper.ExtractDataType(propertytype);
            var relatedObject =  ApplicationHelper.ClassObjectList.Where(x=>x.Name == propertydatatype).FirstOrDefault();
            return relatedObject;
        }

        private static void GenerateLinkingEntityConfiguration(ClassProperty property)
        {
            string EntityName = property.DataUsesAtt.LinkingTable;
            string key1 = $"{property.PropertyNameSingular}Id";
            string key2 = $"{property.DataUsesAtt.InverseProperty.Singularize()}Id";
            CSharpClassObject modalClassObject = ApplicationHelper.ClassObjectList.Where(x => x.Name == EntityName).FirstOrDefault();

            string relativeTargetPath = $"Persistence/Configurations/{EntityName}Configuration.cs";
            FileInfo? targetFile = Helper.GetFileInfo(relativeTargetPath, ApplicationHelper.InfrastructureProjectDirectory);
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
                string efConfigurations = GenerateConfigurations(modalClassObject,key1,key2);

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
                    efconfigurations = efConfigurations,
                };

                // Parse and render the class template
                var classTemplate = Template.Parse(templateContent);
                string generatedClass = classTemplate.Render(masterdata);
                
                generatedClass = generatedClass.Replace("builder.Ignore(e => e.DomainEvents);", "");

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
        
        private static string GenerateConfigurations(CSharpClassObject modalClassObject,string key1,string key2)
        {
            // Build Configurations
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"builder.ToTable(\"{modalClassObject.Name}\");");
            sb.AppendLine($"builder.HasKey(x => new {{x.{key1},x.{key2}}});");
            string efConfigurations = sb.ToString();

            return efConfigurations;
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
                if (property.DataUsesAtt.PrimaryRole == "Relationship")
                {
                    if (property.DataUsesAtt.RelationshipType == "ManyToMany")
                    {
                        string EntityName = property.DataUsesAtt.LinkingTable;
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
