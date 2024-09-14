using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace CleanArchitecture.CodeGenerator.Models
{
    /// <summary>
    /// Represents a C# class structure, including its namespace, name, base class information, and properties.
    /// This class is used to generate intellisense metadata, aiding developers in understanding and utilizing
    /// the class effectively within the Clean Architecture framework.
    /// </summary>
    public class CSharpClassObject
    {
        /// <summary>
        /// Gets or sets the namespace of the C# class.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the C# class.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the base class, if any.
        /// </summary>
        public string BaseNamespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the base class, if any.
        /// </summary>
        public string BaseName { get; set; }

        /// <summary>
        /// Gets or sets the full name of the C# class, including its namespace.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object represents an enum.
        /// </summary>
        public bool IsEnum { get; set; }

        /// <summary>
        /// Gets or sets a summary description of the C# class, typically extracted from XML comments.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the list of properties defined in the C# class.
        /// </summary>
        public List<ClassProperty> Properties { get; set; } = new List<ClassProperty>();
    }

    /// <summary>
    /// Represents a property within a C# class, including its name, type, summary, and optional initialization expression.
    /// This class contributes to generating intellisense metadata for class properties.
    /// </summary>
    public class ClassProperty
    {
        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the type of the property, represented by the <see cref="PropertyType"/> class.
        /// </summary>
        public PropertyType Type { get; set; }

        /// <summary>
        /// Gets or sets a summary description of the property, typically extracted from XML comments.
        /// </summary>
        public string Summary { get; set; }

 
        /// <summary>
        /// Gets or sets the syntax representation of a property declaration in a C# class.
        /// This is an instance of <see cref="PropertyDeclarationSyntax"/>, which represents
        /// the code structure for a property, including its attributes, modifiers, type, and accessors.
        /// This property is used to analyze and manipulate the syntax of properties within the class.
        /// </summary>
        public PropertyDeclarationSyntax propertyDeclarationSyntax { get; set; }

        public string DisplayName { get; set; }
        public string Description { get; set; }

        public string PropRole { get; set; }
        public string RelationshipType { get; set; }
        public string DeleteBehavior { get; set;}
        public string InverseProperty { get; set; }
        public string ForeignKeyProperty { get; set; }
    }

 
    public class PropertyType
    {
        public string TypeName { get; set; }
        public bool IsArray { get; set; }
        public bool IsList { get; set; }
        public bool IsICollection { get; set; }
        public bool IsIEnumerable { get; set; }
        public bool IsDictionary { get; set; }
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the type is a known type, such as a primitive type or a recognized base class.
        /// </summary>
        public bool IsKnownType { get; set; }

    }
}
