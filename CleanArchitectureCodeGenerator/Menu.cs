using CleanArchitecture.CodeGenerator.CodeWriter;
using CleanArchitecture.CodeGenerator.Configuration;

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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=============================================================");
                Console.WriteLine("                         MAIN MENU                            ");
                Console.WriteLine("=============================================================");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n1. Check Configurations");
                Console.WriteLine("\n2. Add New Scaffold Feature");
                Console.WriteLine("\n3. Remove Feature (This will delete Feature files)");
                Console.WriteLine("\n4. Test (For Developemnt Purpose Only!))");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nEnter the number of the entity (or 'q' to quit):");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("\nSelect an option: ");
                Console.ResetColor();

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
                        await Test();
                        break;
                    case "q":
                        exit = true;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid selection. Please try again.");
                        Console.ResetColor();
                        Pause();
                        break;
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Exiting the program...");
            Console.ResetColor();
        }

        private async Task CheckConfigurations()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Check Configurations");
            Console.ResetColor();

            ConfigurationHandler configurationHandler = new ConfigurationHandler("appsettings.json");
            configurationHandler.PrintConfiguration();

            Pause();
        }

        private async Task ScaffoldFeature()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Scaffold Feature");
            Console.ResetColor();

            // Run the code generator.
            CodeEngine codeGenerator = new CodeEngine();
            codeGenerator.Run();
            Pause();
        }

        private async Task DeleteFeature()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Delete Feature");
            Console.ResetColor();

            CodeRemover codeRemover = new CodeRemover();
            codeRemover.Run();
            Pause();
        }

        private async Task Test()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Test Feature");
            Console.ResetColor();

            var rootDirectory = @"D:\CleanArchitectureWithBlazorServer-main\src";
            var entityName = "NewEntity";

            //////Update_DbContext dbContextModifier = new Update_DbContext();
            //////var paths = dbContextModifier.SearchDbContextFiles(rootDirectory);

            //////// Add a property
            //////dbContextModifier.AddEntityProperty(paths, entityName);

            ////////Remove a property
            //////dbContextModifier.RemoveEntityProperty(paths, entityName);

            DemoEntityScaffolder scaffolder = new DemoEntityScaffolder();
            scaffolder.AddMasterDemoEntity();




            Pause();
        }

        private void Pause()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\nPress any key to return to the main menu...");
            Console.ResetColor();
            Console.ReadKey();
        }



    }
}
