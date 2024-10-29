using CleanArchitecture.CodeGenerator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CleanArchitecture.CodeGenerator.CodeWriter
{
    public class Update_DbContext
    {
        public List<string> SearchDbContextFiles(string rootDirectory)
        {
            var filePaths = Directory.GetFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => file.EndsWith("IApplicationDbContext.cs") || file.EndsWith("ApplicationDbContext.cs"))
                .ToList();

           // Console.WriteLine($"Found {filePaths.Count} DbContext files.");
            return filePaths;
        }

        public void AddEntityProperty(List<string> filePaths, string entityName)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    //Console.WriteLine($"Processing file: {filePath}");
                    var code = File.ReadAllText(filePath);
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot() as CompilationUnitSyntax;

                    if (root == null)
                    {
                        throw new InvalidOperationException("Could not parse the root syntax node.");
                    }

                    if (filePath.EndsWith("IApplicationDbContext.cs"))
                    {
                        AddPropertyToInterface(root, filePath, entityName);
                    }
                    else if (filePath.EndsWith("ApplicationDbContext.cs"))
                    {
                        AddPropertyToClass(root, filePath, entityName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        private void AddPropertyToInterface(CompilationUnitSyntax root, string filePath, string entityName)
        {
            var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
            if (interfaceDeclaration != null)
            {
                var pluralizedEntityName = Utility.Pluralize(entityName);

                // Check if the property already exists
                var existingProperty = interfaceDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(prop => prop.Identifier.Text == pluralizedEntityName);

                if (existingProperty == null)
                {
                    var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                                                    SyntaxFactory.GenericName(
                                                        SyntaxFactory.Identifier("DbSet"))
                                                        .WithTypeArgumentList(
                                                            SyntaxFactory.TypeArgumentList(
                                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                                    SyntaxFactory.IdentifierName(entityName))))
                                                    ,
                                                    SyntaxFactory.Identifier(pluralizedEntityName))
                                                .WithAccessorList(SyntaxFactory.AccessorList(
                                                    SyntaxFactory.List(new AccessorDeclarationSyntax[]{
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                                    })));

                    var newInterfaceDeclaration = interfaceDeclaration.AddMembers(propertyDeclaration);
                    var newRoot = root.ReplaceNode(interfaceDeclaration, newInterfaceDeclaration);

                    File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Added property {pluralizedEntityName} to interface {interfaceDeclaration.Identifier.Text} in {filePath}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Property {pluralizedEntityName} already exists in interface {interfaceDeclaration.Identifier.Text}.");
                    Console.ResetColor();
                }
            }
        }

        private void AddPropertyToClass(CompilationUnitSyntax root, string filePath, string entityName)
        {
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration != null)
            {
                var pluralizedEntityName = Utility.Pluralize(entityName);

                // Check if the property already exists
                var existingProperty = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(prop => prop.Identifier.Text == pluralizedEntityName);

                if (existingProperty == null)
                {
                    var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                                                    SyntaxFactory.GenericName(
                                                        SyntaxFactory.Identifier("DbSet"))
                                                        .WithTypeArgumentList(
                                                            SyntaxFactory.TypeArgumentList(
                                                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                                    SyntaxFactory.IdentifierName(entityName))))
                                                    ,
                                                    SyntaxFactory.Identifier(pluralizedEntityName))
                                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                .WithAccessorList(SyntaxFactory.AccessorList(
                                                    SyntaxFactory.List(new AccessorDeclarationSyntax[]{
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                                    })));

                    var newClassDeclaration = classDeclaration.AddMembers(propertyDeclaration);
                    var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

                    File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Added property {pluralizedEntityName} to class {classDeclaration.Identifier.Text} in {filePath}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Property {pluralizedEntityName} already exists in class {classDeclaration.Identifier.Text}.");
                    Console.ResetColor();
                }
            }
        }

        public void RemoveEntityProperty(List<string> filePaths, string entityName)
        {
            foreach (var filePath in filePaths)
            {
                try
                {
                    Console.WriteLine($"Processing file: {filePath}");
                    var code = File.ReadAllText(filePath);
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot() as CompilationUnitSyntax;

                    if (root == null)
                    {
                        throw new InvalidOperationException("Could not parse the root syntax node.");
                    }

                    if (filePath.EndsWith("IApplicationDbContext.cs"))
                    {
                        RemovePropertyFromInterface(root, filePath, entityName);
                    }
                    else if (filePath.EndsWith("ApplicationDbContext.cs"))
                    {
                        RemovePropertyFromClass(root, filePath, entityName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                }
            }
        }

        private void RemovePropertyFromInterface(CompilationUnitSyntax root, string filePath, string entityName)
        {
            var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
            if (interfaceDeclaration != null)
            {
                var pluralizedEntityName = Utility.Pluralize(entityName);

                var propertyToRemove = interfaceDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(prop => prop.Identifier.Text == pluralizedEntityName);

                if (propertyToRemove != null)
                {
                    var newInterfaceDeclaration = interfaceDeclaration.RemoveNode(propertyToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                    var newRoot = root.ReplaceNode(interfaceDeclaration, newInterfaceDeclaration);

                    File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
                    Console.WriteLine($"Removed property {pluralizedEntityName} from interface {interfaceDeclaration.Identifier.Text} in {filePath}");
                }
                else
                {
                    Console.WriteLine($"Property {pluralizedEntityName} not found in interface {interfaceDeclaration.Identifier.Text}.");
                }
            }
        }

        private void RemovePropertyFromClass(CompilationUnitSyntax root, string filePath, string entityName)
        {
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration != null)
            {
                var pluralizedEntityName = Utility.Pluralize(entityName);

                var propertyToRemove = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(prop => prop.Identifier.Text == pluralizedEntityName);

                if (propertyToRemove != null)
                {
                    var newClassDeclaration = classDeclaration.RemoveNode(propertyToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                    var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

                    File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
                    Console.WriteLine($"Removed property {pluralizedEntityName} from class {classDeclaration.Identifier.Text} in {filePath}");
                }
                else
                {
                    Console.WriteLine($"Property {pluralizedEntityName} not found in class {classDeclaration.Identifier.Text}.");
                }
            }
        }
    }
}
