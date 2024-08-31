using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    /// <summary>
    /// Main class responsible for generating code files based on user input and templates.
    /// </summary>
    public class CodeEngine
    {
        private string RootDirectory { get; set; }
        private string RootNamespace { get; set; }
        private string DomainProject { get; set; }
        private string UiProject { get; set; }
        private string InfrastructureProject { get; set; }
        private string ApplicationProject { get; set; }

        public CodeEngine()
        {
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

        public async Task RunAsync()
        {
            var Directory_Domain_Project = Path.Combine(RootDirectory, DomainProject);
            var Directory_Infrastructure_Project = Path.Combine(RootDirectory, InfrastructureProject);
            var Directory_IU_Project = Path.Combine(RootDirectory, UiProject);
            var Directory_Application_Project = Path.Combine(RootDirectory, ApplicationProject);


            var includes = new string[] { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            var objectList = Utility.GetEntities(Directory_Domain_Project)
                .Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name));
            var entities = objectList.Select(x => x.Name).Distinct().ToArray();

            while (true)
            {
                // Display the menu of entities
                Console.WriteLine("Please select an entity to generate by entering the corresponding number:");
                for (int i = 0; i < entities.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {entities[i]}");
                }
                Console.WriteLine("Enter the number of the entity (or 'q' to quit):");

                // Get user input
                string input = Console.ReadLine().Trim();

                // Check if the user wants to quit
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting.");
                    return;
                }

                // Validate input
                if (int.TryParse(input, out int selectedIndex) && selectedIndex > 0 && selectedIndex <= entities.Length)
                {
                    string selectedEntity = entities[selectedIndex - 1];
                    Console.WriteLine($"You selected: {selectedEntity}");

                    // Process the selected entity
                    string[] parsedInputs = Utility.GetParsedInput(selectedEntity);

                    foreach (string inputName in parsedInputs)
                    {
                        try
                        {
                            var ModalClassName = Path.GetFileNameWithoutExtension(inputName);
                            var ModalClassNamePlural = Utility.Pluralize(ModalClassName);
                            var ModalClassObject = objectList.First(x => x.Name == ModalClassName);

                            var events = new List<string>
                            {
                                $"Events/{ModalClassName}CreatedEvent.cs",
                                $"Events/{ModalClassName}DeletedEvent.cs",
                                $"Events/{ModalClassName}UpdatedEvent.cs",
                            };
                            foreach (var TargetClassPath in events)
                            {
                                await AddFileAsync(ModalClassObject, TargetClassPath, ModalClassName, Directory_Domain_Project);
                            }

                            var configurations = new List<string>
                            {
                                $"Persistence/Configurations/{ModalClassName}Configuration.cs",
                                $"PermissionSet/{ModalClassNamePlural}.cs"
                            };
                            foreach (var TargetClassPath in configurations)
                            {
                                await AddFileAsync(ModalClassObject, TargetClassPath, ModalClassName, Directory_Infrastructure_Project);
                            }

                            var list = new List<string>
                            {
                                $"Features/{ModalClassNamePlural}/Commands/AddEdit/AddEdit{ModalClassName}Command.cs",
                                $"Features/{ModalClassNamePlural}/Commands/AddEdit/AddEdit{ModalClassName}CommandValidator.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Create/Create{ModalClassName}Command.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Create/Create{ModalClassName}CommandValidator.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Delete/Delete{ModalClassName}Command.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Delete/Delete{ModalClassName}CommandValidator.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Update/Update{ModalClassName}Command.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Update/Update{ModalClassName}CommandValidator.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Import/Import{ModalClassNamePlural}Command.cs",
                                $"Features/{ModalClassNamePlural}/Commands/Import/Import{ModalClassNamePlural}CommandValidator.cs",
                                $"Features/{ModalClassNamePlural}/Caching/{ModalClassName}CacheKey.cs",
                                $"Features/{ModalClassNamePlural}/DTOs/{ModalClassName}Dto.cs",
                                $"Features/{ModalClassNamePlural}/EventHandlers/{ModalClassName}CreatedEventHandler.cs",
                                $"Features/{ModalClassNamePlural}/EventHandlers/{ModalClassName}UpdatedEventHandler.cs",
                                $"Features/{ModalClassNamePlural}/EventHandlers/{ModalClassName}DeletedEventHandler.cs",
                                $"Features/{ModalClassNamePlural}/Specifications/{ModalClassName}AdvancedFilter.cs",
                                $"Features/{ModalClassNamePlural}/Specifications/{ModalClassName}AdvancedSpecification.cs",
                                $"Features/{ModalClassNamePlural}/Specifications/{ModalClassName}ByIdSpecification.cs",
                                $"Features/{ModalClassNamePlural}/Queries/Export/Export{ModalClassNamePlural}Query.cs",
                                $"Features/{ModalClassNamePlural}/Queries/GetAll/GetAll{ModalClassNamePlural}Query.cs",
                                $"Features/{ModalClassNamePlural}/Queries/GetById/Get{ModalClassName}ByIdQuery.cs",
                                $"Features/{ModalClassNamePlural}/Queries/Pagination/{ModalClassNamePlural}PaginationQuery.cs",
                            };
                            foreach (var TargetClassPath in list)
                            {
                                await AddFileAsync(ModalClassObject, TargetClassPath, ModalClassName, Directory_Application_Project);
                            }

                            var pages = new List<string>
                            {
                                $"Pages/{ModalClassNamePlural}/{ModalClassNamePlural}.razor",
                                $"Pages/{ModalClassNamePlural}/Components/{ModalClassName}FormDialog.razor",
                                $"Pages/{ModalClassNamePlural}/Components/{ModalClassNamePlural}AdvancedSearchComponent.razor"
                            };
                            foreach (var TargetClassPath in pages)
                            {
                                await AddFileAsync(ModalClassObject, TargetClassPath, ModalClassName, Directory_IU_Project);
                            }

                            Console.WriteLine($"Successfully generated files for {ModalClassName}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating file '{inputName}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }


        private async Task AddFileAsync(CSharpClassObject ModalClassObject, string TargetClassPath, string ModalClassName, string TargetProjectDirectory)
        {
            var isValid = Utility.ValidatePath(TargetClassPath, TargetProjectDirectory);
            if (!isValid) { return; }

            FileInfo file = new FileInfo(Path.Combine(TargetProjectDirectory, TargetClassPath));

            if (!file.Exists)
            {
                try
                {
                    TemplateMapper templateMapper = new TemplateMapper();
                    string template = await templateMapper.GenerateClass(ModalClassObject, file.FullName, ModalClassName, TargetProjectDirectory);

                    if (!string.IsNullOrEmpty(template))
                    {
                        await Utility.WriteToDiskAsync(file.FullName, template);
                    }

                    Console.WriteLine($"Created file: {file.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error GetTemplateFilePathAsync: {ex.InnerException.Message}");
                }
            }
            else
            {
                Console.WriteLine($"The file '{file}' already exists.");
            }
        }

    }
}
