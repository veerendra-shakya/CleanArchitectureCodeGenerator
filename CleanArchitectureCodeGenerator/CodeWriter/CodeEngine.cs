using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    /// <summary>
    /// Main class responsible for generating code files based on user input and templates.
    /// </summary>
    public class CodeEngine
    {
        private readonly string _rootDirectory;
        private readonly string _rootNamespace;
        private readonly string _domainProject;
        private readonly string _uiProject;
        private readonly string _infrastructureProject;
        private readonly string _applicationProject;

        public CodeEngine()
        {
            var configHandler = new ConfigurationHandler("appsettings.json");
            var configSettings = configHandler.GetConfiguration();

            _rootDirectory = configSettings.RootDirectory;
            _rootNamespace = configSettings.RootNamespace;
            _domainProject = configSettings.DomainProject;
            _uiProject = configSettings.UiProject;
            _infrastructureProject = configSettings.InfrastructureProject;
            _applicationProject = configSettings.ApplicationProject;
        }

        public async Task RunAsync()
        {
            string domainProjectDir = Path.Combine(_rootDirectory, _domainProject);
            string infrastructureProjectDir = Path.Combine(_rootDirectory, _infrastructureProject);
            string uiProjectDir = Path.Combine(_rootDirectory, _uiProject);
            string applicationProjectDir = Path.Combine(_rootDirectory, _applicationProject);

            string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            var objectList = Utility.GetEntities(domainProjectDir)
                .Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name));
            var entities = objectList.Select(x => x.Name).Distinct().ToArray();

            Console.Clear();
            while (true)
            {
                DisplayEntityMenu(entities);

                string input = Console.ReadLine().Trim();

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting.");
                    return;
                }

                if (int.TryParse(input, out int selectedIndex) && selectedIndex > 0 && selectedIndex <= entities.Length)
                {
                    string selectedEntity = entities[selectedIndex - 1];
                    Console.WriteLine($"You selected: {selectedEntity}");

                    await ProcessEntityAsync(objectList, selectedEntity, domainProjectDir, infrastructureProjectDir, applicationProjectDir, uiProjectDir);
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }

        private static void DisplayEntityMenu(string[] entities)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=============================================================");
            Console.WriteLine("                    ENTITY SELECTION MENU                     ");
            Console.WriteLine("=============================================================");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Please select an entity to generate by entering the corresponding number:");
            Console.ResetColor();

            for (int i = 0; i < entities.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"  {i + 1}. ");
                Console.ResetColor();
                Console.WriteLine(entities[i]);
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\nEnter the number of the entity (or 'q' to quit):");
            Console.ResetColor();
        }



        private async Task ProcessEntityAsync(IEnumerable<CSharpClassObject> objectList, string selectedEntity, string domainProjectDir, string infrastructureProjectDir, string applicationProjectDir, string uiProjectDir)
        {
            string[] parsedInputs = Utility.GetParsedInput(selectedEntity);

            foreach (string inputName in parsedInputs)
            {
                try
                {
                    string modalClassName = Path.GetFileNameWithoutExtension(inputName);
                    string modalClassNamePlural = Utility.Pluralize(modalClassName);
                    var modalClassObject = objectList.First(x => x.Name == modalClassName);

                    await GenerateFilesAsync(modalClassObject, modalClassName, modalClassNamePlural, domainProjectDir, infrastructureProjectDir, applicationProjectDir, uiProjectDir);
                    Console.WriteLine($"Successfully generated files for {modalClassName}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating file '{inputName}': {ex.Message}");
                }
            }
        }

        private async Task GenerateFilesAsync(CSharpClassObject modalClassObject, string modalClassName, string modalClassNamePlural, string domainProjectDir, string infrastructureProjectDir, string applicationProjectDir, string uiProjectDir)
        {
            var eventPaths = new[]
            {
                $"Events/{modalClassName}CreatedEvent.cs",
                $"Events/{modalClassName}DeletedEvent.cs",
                $"Events/{modalClassName}UpdatedEvent.cs"
            };

            var configPaths = new[]
            {
                $"Persistence/Configurations/{modalClassName}Configuration.cs",
                $"PermissionSet/{modalClassNamePlural}.cs"
            };

            var featurePaths = new[]
            {
                $"Features/{modalClassNamePlural}/Commands/AddEdit/AddEdit{modalClassName}Command.cs",
                $"Features/{modalClassNamePlural}/Commands/AddEdit/AddEdit{modalClassName}CommandValidator.cs",
                $"Features/{modalClassNamePlural}/Commands/Create/Create{modalClassName}Command.cs",
                $"Features/{modalClassNamePlural}/Commands/Create/Create{modalClassName}CommandValidator.cs",
                $"Features/{modalClassNamePlural}/Commands/Delete/Delete{modalClassName}Command.cs",
                $"Features/{modalClassNamePlural}/Commands/Delete/Delete{modalClassName}CommandValidator.cs",
                $"Features/{modalClassNamePlural}/Commands/Update/Update{modalClassName}Command.cs",
                $"Features/{modalClassNamePlural}/Commands/Update/Update{modalClassName}CommandValidator.cs",
                $"Features/{modalClassNamePlural}/Commands/Import/Import{modalClassNamePlural}Command.cs",
                $"Features/{modalClassNamePlural}/Commands/Import/Import{modalClassNamePlural}CommandValidator.cs",
                $"Features/{modalClassNamePlural}/Caching/{modalClassName}CacheKey.cs",
                $"Features/{modalClassNamePlural}/DTOs/{modalClassName}Dto.cs",
                $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}CreatedEventHandler.cs",
                $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}UpdatedEventHandler.cs",
                $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}DeletedEventHandler.cs",
                $"Features/{modalClassNamePlural}/Specifications/{modalClassName}AdvancedFilter.cs",
                $"Features/{modalClassNamePlural}/Specifications/{modalClassName}AdvancedSpecification.cs",
                $"Features/{modalClassNamePlural}/Specifications/{modalClassName}ByIdSpecification.cs",
                $"Features/{modalClassNamePlural}/Queries/Export/Export{modalClassNamePlural}Query.cs",
                $"Features/{modalClassNamePlural}/Queries/GetAll/GetAll{modalClassNamePlural}Query.cs",
                $"Features/{modalClassNamePlural}/Queries/GetById/Get{modalClassName}ByIdQuery.cs",
                $"Features/{modalClassNamePlural}/Queries/Pagination/{modalClassNamePlural}PaginationQuery.cs"
            };

            var pagePaths = new[]
            {
                $"Pages/{modalClassNamePlural}/{modalClassNamePlural}.razor",
                $"Pages/{modalClassNamePlural}/Components/{modalClassName}FormDialog.razor",
                $"Pages/{modalClassNamePlural}/Components/{modalClassNamePlural}AdvancedSearchComponent.razor"
            };

            await Task.WhenAll(
                ProcessFilesAsync(modalClassObject, eventPaths, modalClassName, domainProjectDir),
                ProcessFilesAsync(modalClassObject, configPaths, modalClassName, infrastructureProjectDir),
                ProcessFilesAsync(modalClassObject, featurePaths, modalClassName, applicationProjectDir),
                ProcessFilesAsync(modalClassObject, pagePaths, modalClassName, uiProjectDir)
            );
        }

        private async Task ProcessFilesAsync(CSharpClassObject modalClassObject, IEnumerable<string> targetPaths, string modalClassName, string targetProjectDirectory)
        {
            foreach (var targetPath in targetPaths)
            {
                await AddFileAsync(modalClassObject, targetPath, modalClassName, targetProjectDirectory);
            }
        }

        private async Task AddFileAsync(CSharpClassObject modalClassObject, string targetPath, string modalClassName, string targetProjectDirectory)
        {
            if (!Utility.ValidatePath(targetPath, targetProjectDirectory))
            {
                return;
            }

            FileInfo file = new FileInfo(Path.Combine(targetProjectDirectory, targetPath));

            if (file.Exists)
            {
                Console.WriteLine($"The file '{file.FullName}' already exists.");
                return;
            }

            try
            {
                TemplateMapper templateMapper = new TemplateMapper();
                string template = await templateMapper.GenerateClass(modalClassObject, file.FullName, modalClassName, targetProjectDirectory);

                if (!string.IsNullOrEmpty(template))
                {
                    await Utility.WriteToDiskAsync(file.FullName, template);
                    Console.WriteLine($"Created file: {file.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating file '{file.FullName}': {ex.Message}");
            }
        }
    }
}
