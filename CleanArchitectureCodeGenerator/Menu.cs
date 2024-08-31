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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=============================================================");
                Console.WriteLine("                         MAIN MENU                            ");
                Console.WriteLine("=============================================================");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("1. Check Configurations");
                Console.WriteLine("2. Scaffold Feature");
                Console.WriteLine("3. Delete Feature");
                Console.WriteLine("4. Test Feature");
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
                        await DbContextModifier_fun();
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
            await codeGenerator.RunAsync();
            Pause();
        }

        private async Task DeleteFeature()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Delete Feature");
            Console.ResetColor();

            CodeRemover codeRemover = new CodeRemover();
            await codeRemover.RunAsync();
            Pause();
        }

        private async Task DbContextModifier_fun()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("You selected: Test Feature");
            Console.ResetColor();

            var rootDirectory = @"D:\CleanArchitectureWithBlazorServer-main\src";
            var entityName = "NewEntity";

            DbContextModifier dbContextModifier = new DbContextModifier();
            var paths = dbContextModifier.SearchDbContextFiles(rootDirectory);

            // Add a property
            //dbContextModifier.AddEntityProperty(paths, entityName);

            //Remove a property
            dbContextModifier.RemoveEntityProperty(paths, entityName);

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
