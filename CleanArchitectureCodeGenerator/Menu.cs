using CleanArchitecture.CodeGenerator.CodeWriter;

namespace CleanArchitecture.CodeGenerator
{
    public class Menu
    {
        public async void Show()
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Main Menu:");
                Console.WriteLine("1. Check Configurations");
                Console.WriteLine("2. Scaffold Feature");
                Console.WriteLine("3. Delete Feature");
                Console.WriteLine("10. test");

                Console.WriteLine("4. Exit");
                Console.Write("Select an option (1-4): ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await CheckConfigurations();
                        break;
                    case "2":
                        await ScaffoldFeature();
                        break;
                    case "3":
                        await DeleteFeature();
                        break;
                    case "4":
                        exit = true;
                        break;
                    case "10":
                        await DbContextModifier_fun();
                        break;
                    default:
                        Console.WriteLine("Invalid selection. Please try again.");
                        Pause();
                        break;
                }
            }

            Console.WriteLine("Exiting the program...");
        }



        private async Task CheckConfigurations()
        {
            Console.WriteLine("You selected Option 2.");
            Pause();
        }

        private async Task ScaffoldFeature()
        {
            // Run the code generator.
            CodeEngine codeGenerator = new CodeEngine();
            await codeGenerator.RunAsync();
            Pause();
        }

        private async Task DeleteFeature()
        {
            Console.WriteLine("You selected Option 3.");
            await CodeRemover.RunAsync();
            Pause();
        }

        private async Task DbContextModifier_fun()
        {
            Console.WriteLine("You selected Option 3.");
            // var rootDirectory = @"D:\CleanArchitectureWithBlazorServer-main\src"; 
            //var entityName = "NewEntity";

            //var paths = DbContextModifier.SearchDbContextFiles(rootDirectory);

            //// Add a property
            //DbContextModifier.AddEntityProperty(paths, entityName);
            //DbContextModifier.AddEntityProperty(paths, entityName);
            //DbContextModifier.AddEntityProperty(paths, entityName);

            // Remove a property
            //DbContextModifier.RemoveEntityProperty(paths, entityName);
            //DbContextModifier.RemoveEntityProperty(paths, entityName);
            Pause();
        }

        private void Pause()
        {
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey();
        }
    }
}
