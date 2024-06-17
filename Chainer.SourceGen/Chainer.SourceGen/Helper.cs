using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Chainer.SourceGen;

public static class Helper
{
    public static string FormatCode(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot().NormalizeWhitespace().ToFullString();

    public static bool HasAttributeWithName(ISymbol it, string attributeName)
    {
        var attributes = it.GetAttributes();
        return attributes.Any(ad => ad.AttributeClass?.Name == attributeName);
    }

    public static ImmutableArray<IPropertySymbol> GetPropertiesWithAttribute(ITypeSymbol namedTypeSymbol, string attributeName)
    {
        return namedTypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => HasAttributeWithName(x, attributeName))
            .ToImmutableArray();
    }

    public static string GetClassAccessibility(Compilation compilation, BaseTypeDeclarationSyntax classDeclarationSyntax)
    {
        return ModelExtensions.GetDeclaredSymbol(compilation
                    .GetSemanticModel(classDeclarationSyntax.SyntaxTree), classDeclarationSyntax)
                ?.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                _ or null => ""
            };
    }

    public static bool ClassHasAttribute(SyntaxNode syntaxNode, string attributeName)
    {
        return syntaxNode is ClassDeclarationSyntax c &&
               c.AttributeLists.Any(x => x.Attributes.Any(a => a.Name.ToString() == attributeName));
    }

    public static AttributeData GetAttributeWithName(ISymbol it, string attributeName)
    {
        return it.GetAttributes()
            .First(ad => ad.AttributeClass?.Name == attributeName);
    }
}