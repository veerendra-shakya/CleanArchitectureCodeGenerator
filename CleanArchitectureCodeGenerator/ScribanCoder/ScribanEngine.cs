using CleanArchitecture.CodeGenerator.CodeWriter;
using CleanArchitecture.CodeGenerator.CodeWriter.Snippets;
using CleanArchitecture.CodeGenerator.Helpers;
using CleanArchitecture.CodeGenerator.Models;

namespace CleanArchitecture.CodeGenerator.ScribanCoder
{
    public static class ScribanEngine
    {
        static List<CSharpClassObject>? KnownModelsList = new List<CSharpClassObject>();
        static ScribanEngine()
        {
            string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };
            KnownModelsList = ApplicationHelper.ClassObjectList?
            .Where(x => x.BaseClassNames != null && includes != null && includes.Any(baseName => x.BaseClassNames.Contains(baseName)) && !includes.Contains(x.Name))
            .ToList();

        }

        public static void GenerateAll_EF_Configurations(bool force = false)
        {
            if(KnownModelsList != null)
            {
                foreach (var model in KnownModelsList)
                {
                    Console.WriteLine($"---> Start - Entity Framework Configuration Files for: {model.Name}");

                    Infrastructure.Persistence.Configurations.Configuration.Generate(model, $"Persistence/Configurations/{model.Name}Configuration.cs", ApplicationHelper.InfrastructureProjectDirectory, force);

                    //check if any property having many to many relationship then create linking table configurations
                    Ef_RelationshipConfigurationsGenerator.GenerateConfigurations(model);

                    Update_DbContext dbContextModifier = new Update_DbContext();
                    var paths = dbContextModifier.SearchDbContextFiles(ApplicationHelper.RootDirectory);
                    dbContextModifier.AddEntityProperty(paths, model.Name);

                    Console.WriteLine($"---X End - Entity Framework Configuration Files for: {model.Name}\n");
                }
            }
        }
        
        public static void GenerateAll_Features(bool force = false)
        {
            if (KnownModelsList != null)
            {
                foreach (var model in KnownModelsList)
                {
                    Console.WriteLine($"---> Start - Creating Feature Files for: {model.Name}");

                    #region Scriban Coder Domain
                    Domain.Events.CreatedEvent.Generate(
                        model,$"Events/{model.Name}CreatedEvent.cs",
                        ApplicationHelper.DomainProjectDirectory,force);

                    Domain.Events.DeletedEvent.Generate(
                       model,
                       $"Events/{model.Name}DeletedEvent.cs",
                       ApplicationHelper.DomainProjectDirectory, force);

                    Domain.Events.UpdatedEvent.Generate(
                       model,
                       $"Events/{model.Name}UpdatedEvent.cs",
                       ApplicationHelper.DomainProjectDirectory, force);
                    #endregion

                    #region Scriban Coder Application
                    Application.Features.Commands.AddEdit.AddEditCommand.Generate(
                        model,
                        $"Features/{model.NamePlural}/Commands/AddEdit/AddEdit{model.Name}Command.cs",
                        ApplicationHelper.ApplicationProjectDirectory, force);

                    Application.Features.Commands.AddEdit.AddEditCommandValidator.Generate(
                        model,
                        $"Features/{model.NamePlural}/Commands/AddEdit/AddEdit{model.Name}CommandValidator.cs",
                        ApplicationHelper.ApplicationProjectDirectory, force);

                    Application.Features.Commands.Create.CreateCommand.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Create/Create{model.Name}Command.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);

                    Application.Features.Commands.Create.CreateCommandValidator.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Create/Create{model.Name}CommandValidator.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Delete.DeleteCommand.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Delete/Delete{model.Name}Command.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Delete.DeleteCommandValidator.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Delete/Delete{model.Name}CommandValidator.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Update.UpdateCommand.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Update/Update{model.Name}Command.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Update.UpdateCommandValidator.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Update/Update{model.Name}CommandValidator.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Import.ImportCommand.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Import/Import{model.NamePlural}Command.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Commands.Import.ImportCommandValidator.Generate(
                       model,
                       $"Features/{model.NamePlural}/Commands/Import/Import{model.NamePlural}CommandValidator.cs",
                       ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Caching.CacheKey.Generate(
                        model,
                        $"Features/{model.NamePlural}/Caching/{model.Name}CacheKey.cs",
                        ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.DTOs.Dto.Generate(
                         model,
                         $"Features/{model.NamePlural}/DTOs/{model.Name}Dto.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.EventHandlers.CreatedEventHandler.Generate(
                         model,
                         $"Features/{model.NamePlural}/EventHandlers/{model.Name}CreatedEventHandler.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.EventHandlers.UpdatedEventHandler.Generate(
                        model,
                        $"Features/{model.NamePlural}/EventHandlers/{model.Name}UpdatedEventHandler.cs",
                        ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.EventHandlers.DeletedEventHandler.Generate(
                         model,
                         $"Features/{model.NamePlural}/EventHandlers/{model.Name}DeletedEventHandler.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Specifications.AdvancedFilter.Generate(
                         model,
                         $"Features/{model.NamePlural}/Specifications/{model.Name}AdvancedFilter.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Specifications.AdvancedSpecification.Generate(
                         model,
                         $"Features/{model.NamePlural}/Specifications/{model.Name}AdvancedSpecification.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Specifications.ByIdSpecification.Generate(
                         model,
                         $"Features/{model.NamePlural}/Specifications/{model.Name}ByIdSpecification.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Queries.Export.ExportQuery.Generate(
                         model,
                         $"Features/{model.NamePlural}/Queries/Export/Export{model.NamePlural}Query.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Queries.GetAll.GetAllQuery.Generate(
                         model,
                         $"Features/{model.NamePlural}/Queries/GetAll/GetAll{model.NamePlural}Query.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Queries.GetById.GetByIdQuery.Generate(
                         model,
                         $"Features/{model.NamePlural}/Queries/GetById/Get{model.Name}ByIdQuery.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Features.Queries.Pagination.PaginationQuery.Generate(
                         model,
                         $"Features/{model.NamePlural}/Queries/Pagination/{model.NamePlural}PaginationQuery.cs",
                         ApplicationHelper.ApplicationProjectDirectory, force);


                    Application.Common.Interfaces.DataAccess.IService.Generate(
                          model,
                          $"Common/Interfaces/DataAccess/I{model.Name}Service.cs",
                          ApplicationHelper.ApplicationProjectDirectory, force);

                    #endregion

                    #region Scriban Coder Infrastructure
                    Infrastructure.PermissionSet.PermissionSet.Generate(
                        model,
                        $"PermissionSet/{model.NamePlural}.cs",
                        ApplicationHelper.InfrastructureProjectDirectory, force);


                    Infrastructure.Persistence.Configurations.Configuration.Generate(
                         model,
                         $"Persistence/Configurations/{model.Name}Configuration.cs",
                         ApplicationHelper.InfrastructureProjectDirectory, force);


                    Infrastructure.Services.DataAccess.Service.Generate(
                          model,
                          $"Services/DataAccess/{model.Name}Service.cs",
                          ApplicationHelper.InfrastructureProjectDirectory, force);

                    #endregion


                    Console.WriteLine($"---X End - of Feature Files for: {model.Name}\n");
                }
            }
        }

        public static void GenerateAll_UI(bool force = false)
        {
            if (KnownModelsList != null)
            {
                foreach (var model in KnownModelsList)
                {
                    Console.WriteLine($"---> Start - Creating Feature Files for: {model.Name}");

                    #region Scriban Coder UiProject
                    ScribanCoder.UI.Controllers.API_Controller.Generate(
                        model,
                        $"Controllers/{model.Name}Controller.cs",
                        ApplicationHelper.UiProjectDirectory, force);


                    ScribanCoder.UI.Components.Autocompletes.AutocompleteRazorComponent.Generate(
                        model,
                        $"Components/Autocompletes/{model.Name}Autocomplete.razor.cs",
                        ApplicationHelper.UiProjectDirectory, force);


                    ScribanCoder.UI.Pages.ListPage_razor.Generate(
                        model,
                        $"Pages/{model.NamePlural}/{model.NamePlural}.razor",
                        ApplicationHelper.UiProjectDirectory, force);


                    ScribanCoder.UI.Pages.Components.AdvancedSearch_razor.Generate(
                        model,
                        $"Pages/{model.NamePlural}/Components/{model.NamePlural}AdvancedSearchComponent.razor",
                        ApplicationHelper.UiProjectDirectory, force);



                    ScribanCoder.UI.Pages.Components.FormDialog_razor.Generate(
                        model,
                        $"Pages/{model.NamePlural}/Components/{model.Name}FormDialog.razor",
                        ApplicationHelper.UiProjectDirectory, force);


                    var menuItemAdder = new RegisterMenuItemHelper();
                    menuItemAdder.AddMenuItem(model.Name, $"/pages/{model.NamePlural}");

                    #endregion

                    Console.WriteLine($"---X End - of Feature Files for: {model.Name}\n");
                }
            }
        }

    }
}
