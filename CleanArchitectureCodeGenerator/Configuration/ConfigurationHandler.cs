using CleanArchitecture.CodeGenerator.Configuration;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CleanArchitecture.CodeGenerator.Configuration
{
    /// <summary>
    /// Read configuration
    ///  string configFilePath = "appsettings.json";
    ///  var configHandler = new ConfigurationHandler(configFilePath);
    ///  var configSettings = configHandler.GetConfiguration();
    /// </summary>
    public class ConfigurationHandler
    {
        private readonly IConfigurationRoot _configuration;

        public ConfigurationHandler(string filePath)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(filePath, optional: false, reloadOnChange: false)
                .Build();
        }

        public ConfigurationSettings GetConfiguration()
        {
            return _configuration.GetSection("ConfigurationSettings").Get<ConfigurationSettings>();
        }

        public void PrintConfiguration()
        {
            var configSettings = GetConfiguration();
    
            Console.Clear();
            Console.WriteLine("\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=============================================================");
            Console.WriteLine("             Current Configuration Settings                  ");
            Console.WriteLine("=============================================================");
            Console.ResetColor();
            Console.WriteLine("\n");
            PrintConfig("Root Directory", configSettings.RootDirectory, ConsoleColor.Yellow);
            PrintConfig("Root Namespace", configSettings.RootNamespace, ConsoleColor.Yellow);
            PrintConfig("Domain Project", configSettings.DomainProject, ConsoleColor.Yellow);
            PrintConfig("UI Project", configSettings.UiProject, ConsoleColor.Yellow);
            PrintConfig("Infrastructure Project", configSettings.InfrastructureProject, ConsoleColor.Yellow);
            PrintConfig("Application Project", configSettings.ApplicationProject, ConsoleColor.Yellow);
            PrintConfig("OpenAI:ApiKey", configSettings.OpenAIApiKey, ConsoleColor.Yellow);

            Console.WriteLine("\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Note: These configuration settings can be changed in the appsettings.json file.");
            Console.ResetColor();
        }

        private void PrintConfig(string name, string value, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{name}: {value}");
            Console.ResetColor();
        }
    }

}