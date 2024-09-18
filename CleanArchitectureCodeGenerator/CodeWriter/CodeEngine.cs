using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator.ScribanCoder;
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
        private readonly List<CSharpClassObject> KnownModelsList = new List<CSharpClassObject>();
        public CodeEngine()
        {
            string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            // KnownModelsList = ApplicationHelper.ClassObjectList.Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name)).ToList();

            KnownModelsList = ApplicationHelper.ClassObjectList?
             .Where(x => x.BaseName != null && includes != null && includes.Any(baseName => x.BaseName.Contains(baseName)) && !includes.Contains(x.Name))
             .ToList();

        }

        public void Run()
        {
            var entities = KnownModelsList
                  .Where(x => x.Name != "Contact" && x.Name != "Document" && x.Name != "Product")
                  .Select(x => x.Name)
                  .Distinct()
                  .ToArray();

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

                    ProcessEntity(selectedEntity);
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }

        private void DisplayEntityMenu(string[] entities)
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

        private void ProcessEntity(string selectedModel)
        {
            string[] parsedInputs = Utility.GetParsedInput(selectedModel);
            foreach (string inputName in parsedInputs)
            {
                try
                {
                    string modalClassName = Path.GetFileNameWithoutExtension(inputName);
                    string modalClassNamePlural = Utility.Pluralize(modalClassName);
                    var modalClassObject = KnownModelsList.First(x => x.Name == modalClassName);

                    // Validate the class & properties before generating files
                    if (!Utility.IsModelClassValid(modalClassObject))
                    {
                        Console.WriteLine("File generation aborted due to validation errors.");
                        return;
                    }

                    GenerateTargetPaths(modalClassObject, modalClassName, modalClassNamePlural);
                    Console.WriteLine($"Successfully generated files for {modalClassName}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating file '{inputName}': {ex.Message}");
                }
            }
        }

        private void GenerateTargetPaths(CSharpClassObject modalClassObject, string modalClassName, string modalClassNamePlural)
        {
            #region Setup Basic Application Paths
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
            #endregion

            ProcessFiles(modalClassObject, eventPaths, ApplicationHelper.DomainProjectDirectory);
            ProcessFiles(modalClassObject, configPaths, ApplicationHelper.InfrastructureProjectDirectory);
            ProcessFiles(modalClassObject, featurePaths, ApplicationHelper.ApplicationProjectDirectory);
            ProcessFiles(modalClassObject, pagePaths, ApplicationHelper.UiProjectDirectory);

            #region Generate Services for Data Access
            Console.WriteLine($"\n--------------------- Generating Services...  --------------------");
            GenerateCodeFile(modalClassObject, $"Common/Interfaces/I{modalClassName}Service.cs", ApplicationHelper.ApplicationProjectDirectory);
            GenerateCodeFile(modalClassObject, $"Services/DataServices/{modalClassName}Service.cs", ApplicationHelper.InfrastructureProjectDirectory);
           // GenerateCodeFile(modalClassObject, $"Components/Autocompletes/{modalClassName}Autocomplete.razor.cs", ApplicationHelper.UiProjectDirectory);
            
            AutocompleteRazorComponent.Generate(modalClassObject, $"Components/Autocompletes/{modalClassName}Autocomplete.razor.cs", ApplicationHelper.UiProjectDirectory);
            
            Console.WriteLine($"\n--------------------- Services Generated  --------------------");

            #endregion

            #region Update DbContext
            Console.WriteLine($"\n--------------------- {modalClassObject} Update DbContext Started...  --------------------");
                Update_DbContext dbContextModifier = new Update_DbContext();
                var paths = dbContextModifier.SearchDbContextFiles(ApplicationHelper.RootDirectory);
                dbContextModifier.AddEntityProperty(paths, modalClassName);
                Console.WriteLine($"---------------------  Update DbContext Completed...  --------------------\n");
            #endregion

            #region Generate Additional Requirments

                //check if any property having many to many relationship then create linking table configurations
                Ef_LinkingTableConfigurationsGenerator.GenerateConfigurations(modalClassObject);

            #endregion

        }

        private void ProcessFiles(CSharpClassObject modalClassObject, IEnumerable<string> relativeTargetPaths, string targetProjectDirectory)
        {
            Console.WriteLine($"\n---------------------  {Utility.GetProjectNameFromPath(targetProjectDirectory)} Started...  --------------------");
            int count = 1;
            foreach (var targetPath in relativeTargetPaths)
            {
                Console.Write($" {count} of {relativeTargetPaths.Count()}  ");
                GenerateCodeFile(modalClassObject, targetPath, targetProjectDirectory);
                // Add a 0.5-second delay
                Thread.Sleep(200);
                count++;
            }

            Console.WriteLine($"---------------------  {Utility.GetProjectNameFromPath(targetProjectDirectory)} Completed...  --------------------\n");
        }

        public void GenerateCodeFile(CSharpClassObject modalClassObject, string relativeTargetPath, string targetProjectDirectory)
        {
            if (!Utility.ValidatePath(relativeTargetPath, targetProjectDirectory))
            {
                return;
            }

            FileInfo targetFile = new FileInfo(Path.Combine(targetProjectDirectory, relativeTargetPath));
            if (targetFile.Exists)
            {
                Console.WriteLine($"The file '{targetFile.FullName}' already exists.");
                return;
            }

            try
            {
                TemplateMapper templateMapper = new TemplateMapper();
                string template = templateMapper.GenerateClass(modalClassObject, targetFile.FullName, targetProjectDirectory);

                if (!string.IsNullOrEmpty(template))
                {
                    Utility.WriteToDiskAsync(targetFile.FullName, template);
                    Console.WriteLine($"Created file: {targetFile.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating file '{targetFile.FullName}': {ex.Message}");
            }
        }

    }
}
