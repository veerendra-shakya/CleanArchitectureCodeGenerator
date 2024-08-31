using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator;

namespace CleanArchitectureCodeGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=============================================================");
            Console.WriteLine("                   CLEAN ARCHITECTURE CODE GENERATOR          ");
            Console.WriteLine("=============================================================");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Welcome to the Clean Architecture Code Generator!");
            Console.WriteLine("-------------------------------------------------");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("What does this tool do?");
            Console.ResetColor();

            Console.WriteLine("This tool is designed to streamline the development of Clean Architecture applications.");
            Console.WriteLine("It automates the generation of essential code components, ensuring your project");
            Console.WriteLine("follows the best practices and architectural guidelines.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nFeatures:");
            Console.ResetColor();

            Console.WriteLine("- Generates boilerplate code for domain, application, infrastructure, and UI layers.");
            Console.WriteLine("- Ensures separation of concerns with clear boundaries between layers.");
            Console.WriteLine("- Supports customization to fit your specific project needs.");
            Console.WriteLine("- Easy to integrate into your existing workflow.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nHow to use:");
            Console.ResetColor();

            Console.WriteLine("Simply run this tool and follow the on-screen prompts. You'll be guided through");
            Console.WriteLine("the process of generating code for each layer of your application, from");
            Console.WriteLine("entity classes to repository interfaces and beyond.");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nGet Started Now!");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=============================================================");
            Console.WriteLine("                   LET'S BUILD SOMETHING GREAT!               ");
            Console.WriteLine("=============================================================");
            Console.ResetColor();
            Console.WriteLine("\n\n");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Press any key to go to the menu...");
            Console.ResetColor();

            // Wait for the user to press any key
            Console.ReadKey();

            // Show the menu
            Menu menu = new Menu();
            menu.Show();
        }
    }
}
