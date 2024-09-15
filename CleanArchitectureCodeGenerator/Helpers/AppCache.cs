using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public static class AppCache
    {
        // Static field to hold the Class object list
        public static List<CSharpClassObject> ClassObjectList { get; private set; }

        // Static constructor to initialize the object list
        static AppCache()
        {
            LoadObjectList();
        }

        // Method to load the object list
        private static void LoadObjectList()
        {
            var configHandler = new ConfigurationHandler("appsettings.json");
            var configSettings = configHandler.GetConfiguration();
            var _rootDirectory = configSettings.RootDirectory;
            var _domainProject = configSettings.DomainProject;

            string domainProjectDir = Path.Combine(_rootDirectory, _domainProject,"Entities");
           // string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            ClassObjectList = Utility.GetEntities(domainProjectDir)
               // .Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name))
                .ToList();  
        }

        // You can also provide a method to manually refresh the cache if needed
        public static void RefreshCache()
        {
            LoadObjectList();
        }
    }
}
