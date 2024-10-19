using System;
using System.IO;
using System.Text;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public class RegisterMenuItemHelper
    {
        private const string MenuFlag = "//#MenuFlag";  // Marker to insert the new item above this line.

        public void AddMenuItem(string title, string href)
        {
            var menuFilePath = Path.Combine(ApplicationHelper.UiProjectDirectory, "Services", "Navigation", "MenuService.cs");

            if (!File.Exists(menuFilePath))
            {
                throw new FileNotFoundException($"MenuService.cs file not found at path: {menuFilePath}");
            }

            string fileContent = File.ReadAllText(menuFilePath);

            // Check if the Href already exists in the file
            if (fileContent.Contains($"Href = \"{href}\""))
            {
                Console.WriteLine($"---> Menu item with Href '{href}' already exists. Skipping addition.");
                return;
            }

            // Find the position of the menu flag
            int flagIndex = fileContent.IndexOf(MenuFlag);
            if (flagIndex == -1)
            {
                throw new Exception($"---> Menu flag '{MenuFlag}' not found in the file.");
            }

            // Generate the new menu item code
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("                        new()");
            stringBuilder.AppendLine("                        {");
            stringBuilder.AppendLine($"                            Title = \"{title}\",");
            stringBuilder.AppendLine($"                            Href = \"{href}\",");
            stringBuilder.AppendLine($"                            PageStatus = PageStatus.Completed,");
            stringBuilder.AppendLine("                        },");

            string newMenuItemCode = stringBuilder.ToString();

            // Insert the new menu item code above the MenuFlag
            var updatedContent = new StringBuilder(fileContent);
            updatedContent.Insert(flagIndex, newMenuItemCode + Environment.NewLine);

            // Write the updated content back to the MenuService.cs file
            File.WriteAllText(menuFilePath, updatedContent.ToString());
            Console.WriteLine($"---> Menu item with Href '{href}' added.");
        }
    }
}
