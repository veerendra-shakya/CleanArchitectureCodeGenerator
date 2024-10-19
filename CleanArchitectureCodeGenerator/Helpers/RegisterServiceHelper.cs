using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public class RegisterServiceHelper
    {
        private const string Flag = "//#ServiceRegistrationFlag";  // Marker to insert the new code above this line.

        public void Register(string serviceName, string interfaceName)
        {
            // Construct the file path for DependencyInjection.cs
            var FilePath = Path.Combine(ApplicationHelper.InfrastructureProjectDirectory, "DependencyInjection.cs");

            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException($"DependencyInjection.cs file not found at path: {FilePath}");
            }

            string fileContent = File.ReadAllText(FilePath);

            // Check if the Service already exists in the file
            if (fileContent.Contains($"services.AddSingletonWithInit<{serviceName}, {interfaceName}>();"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"---> Service '{serviceName}' already registered. Skipping addition.");
                Console.ResetColor();
                return;
            }

            // Find the position of the menu flag
            int flagIndex = fileContent.IndexOf(Flag);
            if (flagIndex == -1)
            {
                throw new Exception($"---> Service flag '{Flag}' not found in the file.");
            }

            // Generate code
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"        services.AddSingletonWithInit<{serviceName}, {interfaceName}>();");
           
            string newCode = stringBuilder.ToString();

            // Insert the new code above the Flag
            var updatedContent = new StringBuilder(fileContent);
            updatedContent.Insert(flagIndex, newCode + Environment.NewLine);

            // Write the updated content back to the file
            File.WriteAllText(FilePath, updatedContent.ToString());
            Console.ForegroundColor = ConsoleColor.Green;       
            Console.WriteLine($"---> Service Registered: services.AddSingletonWithInit<{serviceName}, {interfaceName}>(); added.");
            Console.WriteLine($"---> File Updated: {FilePath}");
            Console.ResetColor();
        }

    }
}
