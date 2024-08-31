using CleanArchitecture.CodeGenerator.Helpers;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    public static class CodeRemover
    {
        public static string ROOT_DIRECTORY = @"D:\CleanArchitectureWithBlazorServer-main\src";
        public static string ROOT_NAMESPACE = "CleanArchitecture.Blazor";
        public static string DOMAIN_PROJECT = "Domain";
        public static string UI_PROJECT = "Server.UI";
        public static string INFRASTRUCTURE_PROJECT = "Infrastructure";
        public static string APPLICATION_PROJECT = "Application";

        public static async Task RunAsync()
        {
            var Directory_Domain_Project = Path.Combine(ROOT_DIRECTORY, DOMAIN_PROJECT);
            var Directory_Infrastructure_Project = Path.Combine(ROOT_DIRECTORY, INFRASTRUCTURE_PROJECT);
            var Directory_IU_Project = Path.Combine(ROOT_DIRECTORY, UI_PROJECT);
            var Directory_Application_Project = Path.Combine(ROOT_DIRECTORY, APPLICATION_PROJECT);

            var includes = new string[] { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            var objectList = Utility.GetEntities(Directory_Domain_Project)
                .Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name));
            var entities = objectList.Select(x => x.Name).Distinct().ToArray();

            while (true)
            {
                // Display the menu of entities
                Console.WriteLine("Please select an entity to DELETE by entering the corresponding number:");
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
                                DeleteFileIfExists(Path.Combine(Directory_Domain_Project, TargetClassPath));
                            }

                            var configurations = new List<string>
                            {
                                $"Persistence/Configurations/{ModalClassName}Configuration.cs",
                                $"PermissionSet/{ModalClassNamePlural}.cs"
                            };
                            foreach (var TargetClassPath in configurations)
                            {
                                DeleteFileIfExists(Path.Combine(Directory_Infrastructure_Project, TargetClassPath));
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
                                DeleteFileIfExists(Path.Combine(Directory_Application_Project, TargetClassPath));
                            }

                            var pages = new List<string>
                            {
                                $"Pages/{ModalClassNamePlural}/{ModalClassNamePlural}.razor",
                                $"Pages/{ModalClassNamePlural}/Components/{ModalClassName}FormDialog.razor",
                                $"Pages/{ModalClassNamePlural}/Components/{ModalClassNamePlural}AdvancedSearchComponent.razor"
                            };
                            foreach (var TargetClassPath in pages)
                            {
                                DeleteFileIfExists(Path.Combine(Directory_IU_Project, TargetClassPath));
                            }

                            Console.WriteLine($"Successfully DELETE files for {ModalClassName}.");
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


        public static void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"File deleted: {filePath}");
                }
                else
                {
                    Console.WriteLine($"File does not exist: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while trying to delete the file: {ex.Message}");
            }
        }
    }
}
