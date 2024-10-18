using CleanArchitecture.CodeGenerator.Configuration;
using CleanArchitecture.CodeGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Helpers
{
    public static class ApplicationHelper
    {
 
        public static string RootDirectory;
        public static string RootNamespace;
        public static string DomainProjectName;
        public static string UiProjectName;
        public static string InfrastructureProjectName;
        public static string ApplicationProjectName;

        public static string DomainProjectDirectory;
        public static string InfrastructureProjectDirectory;
        public static string UiProjectDirectory;
        public static string ApplicationProjectDirectory;

        // Static field to hold the Class object list
        public static List<CSharpClassObject> ClassObjectList { get; private set; }

        // Static constructor to initialize the object list
        static ApplicationHelper()
        {
            InitializeVariables();
            LoadClassObjectList();
        }

        private static void InitializeVariables()
        {
            var configHandler = new ConfigurationHandler("appsettings.json");
            var configSettings = configHandler.GetConfiguration();

            RootDirectory = configSettings.RootDirectory;
            RootNamespace = configSettings.RootNamespace;
            DomainProjectName = configSettings.DomainProject;
            UiProjectName = configSettings.UiProject;
            InfrastructureProjectName = configSettings.InfrastructureProject;
            ApplicationProjectName = configSettings.ApplicationProject;

            DomainProjectDirectory = Path.Combine(RootDirectory, DomainProjectName);
            InfrastructureProjectDirectory = Path.Combine(RootDirectory, InfrastructureProjectName);
            UiProjectDirectory = Path.Combine(RootDirectory, UiProjectName);
            ApplicationProjectDirectory = Path.Combine(RootDirectory, ApplicationProjectName);
        }

        // Method to load the object list
        private static void LoadClassObjectList()
        {
           
            string domainProjectDir = Path.Combine(RootDirectory, DomainProjectName,"Entities");
           // string[] includes = { "IEntity", "BaseEntity", "BaseAuditableEntity", "BaseAuditableSoftDeleteEntity", "AuditTrail", "OwnerPropertyEntity", "KeyValue" };

            ClassObjectList = Utility.GetEntities(domainProjectDir)
               // .Where(x => includes.Contains(x.BaseName) && !includes.Contains(x.Name))
                .ToList();  
        }

        // You can also provide a method to manually refresh the cache if needed
        public static void Refresh()
        {
            InitializeVariables();
            LoadClassObjectList();
        }
    }
}

