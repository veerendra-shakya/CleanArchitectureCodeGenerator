using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.CodeGenerator.Models
{
    public class CSharpClassObject
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string BaseNamespace { get; set; }
        public string BaseName { get; set; }
        public string FullName { get; set; }
        public bool IsEnum { get; set; }
        public string Summary { get; set; }
        public List<ClassProperty> Properties { get; set; } = new List<ClassProperty>();
        public HashSet<string> References { get; set; }
    }

    public class ClassProperty
    {
        public string Name { get; set; }
        public PropertyType Type { get; set; }
        public string Summary { get; set; }
        public string InitExpression { get; set; }
    }

    public class PropertyType
    {
        public string CodeName { get; set; }
        public bool IsArray { get; set; }
        public bool IsDictionary { get; set; }
        public bool IsOptional { get; set; }
        public string ClientSideReferenceName { get; set; }
        public bool IsKnownType { get; set; }
    }
}
