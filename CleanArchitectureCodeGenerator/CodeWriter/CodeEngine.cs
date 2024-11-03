using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;
using Humanizer;



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
             .Where(x => x.BaseClassNames != null && includes != null && includes.Any(baseName => x.BaseClassNames.Contains(baseName)) && !includes.Contains(x.Name))
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
                    string modalClassNamePlural = modalClassName.Pluralize();
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

            #region Scriban Coder Domain
            ScribanCoder.Domain.Events.CreatedEvent.Generate(
                modalClassObject,
                $"Events/{modalClassName}CreatedEvent.cs",
                ApplicationHelper.DomainProjectDirectory
                );

            ScribanCoder.Domain.Events.DeletedEvent.Generate(
               modalClassObject,
               $"Events/{modalClassName}DeletedEvent.cs",
               ApplicationHelper.DomainProjectDirectory
               );

            ScribanCoder.Domain.Events.UpdatedEvent.Generate(
               modalClassObject,
               $"Events/{modalClassName}UpdatedEvent.cs",
               ApplicationHelper.DomainProjectDirectory
               );
            #endregion

            #region Scriban Coder Application
            ScribanCoder.Application.Features.Commands.AddEdit.AddEditCommand.Generate(
                modalClassObject,
                $"Features/{modalClassNamePlural}/Commands/AddEdit/AddEdit{modalClassName}Command.cs",
                ApplicationHelper.ApplicationProjectDirectory
                );

            ScribanCoder.Application.Features.Commands.AddEdit.AddEditCommandValidator.Generate(
                modalClassObject,
                $"Features/{modalClassNamePlural}/Commands/AddEdit/AddEdit{modalClassName}CommandValidator.cs",
                ApplicationHelper.ApplicationProjectDirectory
                );

            ScribanCoder.Application.Features.Commands.Create.CreateCommand.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Create/Create{modalClassName}Command.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Create.CreateCommandValidator.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Create/Create{modalClassName}CommandValidator.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Delete.DeleteCommand.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Delete/Delete{modalClassName}Command.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Delete.DeleteCommandValidator.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Delete/Delete{modalClassName}CommandValidator.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Update.UpdateCommand.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Update/Update{modalClassName}Command.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Update.UpdateCommandValidator.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Update/Update{modalClassName}CommandValidator.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Import.ImportCommand.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Import/Import{modalClassNamePlural}Command.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Commands.Import.ImportCommandValidator.Generate(
               modalClassObject,
               $"Features/{modalClassNamePlural}/Commands/Import/Import{modalClassNamePlural}CommandValidator.cs",
               ApplicationHelper.ApplicationProjectDirectory
               );

            ScribanCoder.Application.Features.Caching.CacheKey.Generate(
                modalClassObject,
                $"Features/{modalClassNamePlural}/Caching/{modalClassName}CacheKey.cs",
                ApplicationHelper.ApplicationProjectDirectory
                );

            ScribanCoder.Application.Features.DTOs.Dto.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/DTOs/{modalClassName}Dto.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.EventHandlers.CreatedEventHandler.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}CreatedEventHandler.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.EventHandlers.UpdatedEventHandler.Generate(
                modalClassObject,
                $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}UpdatedEventHandler.cs",
                ApplicationHelper.ApplicationProjectDirectory
                );

            ScribanCoder.Application.Features.EventHandlers.DeletedEventHandler.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/EventHandlers/{modalClassName}DeletedEventHandler.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Specifications.AdvancedFilter.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Specifications/{modalClassName}AdvancedFilter.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Specifications.AdvancedSpecification.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Specifications/{modalClassName}AdvancedSpecification.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Specifications.ByIdSpecification.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Specifications/{modalClassName}ByIdSpecification.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Queries.Export.ExportQuery.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Queries/Export/Export{modalClassNamePlural}Query.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Queries.GetAll.GetAllQuery.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Queries/GetAll/GetAll{modalClassNamePlural}Query.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Queries.GetById.GetByIdQuery.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Queries/GetById/Get{modalClassName}ByIdQuery.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Features.Queries.Pagination.PaginationQuery.Generate(
                 modalClassObject,
                 $"Features/{modalClassNamePlural}/Queries/Pagination/{modalClassNamePlural}PaginationQuery.cs",
                 ApplicationHelper.ApplicationProjectDirectory
                 );

            ScribanCoder.Application.Common.Interfaces.DataAccess.IService.Generate(
                  modalClassObject,
                  $"Common/Interfaces/DataAccess/I{modalClassName}Service.cs",
                  ApplicationHelper.ApplicationProjectDirectory
                  );
            #endregion

            #region Scriban Coder Infrastructure
            ScribanCoder.Infrastructure.PermissionSet.PermissionSet.Generate(
                modalClassObject,
                $"PermissionSet/{modalClassNamePlural}.cs",
                ApplicationHelper.InfrastructureProjectDirectory
                );

            ScribanCoder.Infrastructure.Persistence.Configurations.Configuration.Generate(
                 modalClassObject,
                 $"Persistence/Configurations/{modalClassName}Configuration.cs",
                 ApplicationHelper.InfrastructureProjectDirectory
                 );

            ScribanCoder.Infrastructure.Services.DataAccess.Service.Generate(
                  modalClassObject,
                  $"Services/DataAccess/{modalClassName}Service.cs",
                  ApplicationHelper.InfrastructureProjectDirectory
                  );
            #endregion

            #region Scriban Coder UiProject
            ScribanCoder.UI.Controllers.API_Controller.Generate(
                modalClassObject,
                $"Controllers/{modalClassName}Controller.cs",
                ApplicationHelper.UiProjectDirectory
                );

            ScribanCoder.UI.Components.Autocompletes.AutocompleteRazorComponent.Generate(
                modalClassObject,
                $"Components/Autocompletes/{modalClassName}Autocomplete.razor.cs",
                ApplicationHelper.UiProjectDirectory
                );

            ScribanCoder.UI.Pages.ListPage_razor.Generate(
                modalClassObject,
                $"Pages/{modalClassNamePlural}/{modalClassNamePlural}.razor",
                ApplicationHelper.UiProjectDirectory
                );

            ScribanCoder.UI.Pages.Components.AdvancedSearch_razor.Generate(
                modalClassObject,
                $"Pages/{modalClassNamePlural}/Components/{modalClassNamePlural}AdvancedSearchComponent.razor",
                ApplicationHelper.UiProjectDirectory
                );


            ScribanCoder.UI.Pages.Components.FormDialog_razor.Generate(
                modalClassObject,
                $"Pages/{modalClassNamePlural}/Components/{modalClassName}FormDialog.razor",
                ApplicationHelper.UiProjectDirectory
                );
            #endregion

            #region Update DbContext
            Console.WriteLine($"\n--------------------- {modalClassObject} Update DbContext Started...  --------------------");
            Update_DbContext dbContextModifier = new Update_DbContext();
            var paths = dbContextModifier.SearchDbContextFiles(ApplicationHelper.RootDirectory);
            dbContextModifier.AddEntityProperty(paths, modalClassName);
            Console.WriteLine($"---------------------  Update DbContext Completed...  --------------------\n");
            #endregion

            #region Generate Additional Requirments

            var menuItemAdder = new RegisterMenuItemHelper();
            menuItemAdder.AddMenuItem(modalClassName, $"/pages/{modalClassNamePlural}");

            //check if any property having many to many relationship then create linking table configurations
            Ef_RelationshipConfigurationsGenerator.GenerateConfigurations(modalClassObject);

            #endregion

        }

    }
}
