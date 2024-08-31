using CleanArchitecture.CodeGenerator.Models;
using CleanArchitecture.CodeGenerator;

namespace CleanArchitectureCodeGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Menu menu = new Menu();
            menu.Show();
        }
    }
}
