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
    }

}