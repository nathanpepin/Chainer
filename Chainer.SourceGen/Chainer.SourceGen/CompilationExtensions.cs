using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Chainer.SourceGen;

public static class CompilationExtensions
{
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation,
        string fullyQualifiedMetadataName)
    {
        INamedTypeSymbol? type = null;

        foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
        {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
            {
                Debug.Assert(type is null);
                return currentType;
            }

            switch (currentType.GetResultantVisibility())
            {
                case SymbolVisibility.Public:
                case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                case SymbolVisibility.Private:
                default:
                    continue;
            }

            if (type != null)
            {
                // Multiple visible types with the same metadata name are present
                return null;
            }

            type = currentType;
        }

        return type;
    }

    // https://github.com/dotnet/roslyn/blob/d2ff1d83e8fde6165531ad83f0e5b1ae95908289/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs#L28-L73
    private static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
    {
        // Start by assuming it's visible.
        var visibility = SymbolVisibility.Public;
        switch (symbol.Kind)
        {
            case SymbolKind.Alias:
                // Aliases are uber private.  They're only visible in the same file that they
                // were declared in.
                return SymbolVisibility.Private;
            case SymbolKind.Parameter:
                // Parameters are only as visible as their containing symbol
                return GetResultantVisibility(symbol.ContainingSymbol);
            case SymbolKind.TypeParameter:
                // Type Parameters are private.
                return SymbolVisibility.Private;
        }

        while (symbol is not null && symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                // If we see anything private, then the symbol is private.
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return SymbolVisibility.Private;
                // If we see anything internal, then knock it down from public to
                // internal.
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = SymbolVisibility.Internal;
                    break;
                // For anything else (Public, Protected, ProtectedOrInternal), the
                // symbol stays at the level we've gotten so far.
            }

            symbol = symbol.ContainingSymbol;
        }

        return visibility;
    }

    private enum SymbolVisibility
    {
        Public,
        Internal,
        Private,
    }

    public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation compilation,
        string typeMetadataName)
    {
        return compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
            .Where(t => t != null)
            .ToImmutableArray();
    }

    public static bool Implements(INamedTypeSymbol symbol, ITypeSymbol type)
    {
        return symbol.AllInterfaces.Any(type.Equals);
    }

    public static bool InheritsFrom(INamedTypeSymbol symbol, ITypeSymbol type)
    {
        var baseType = symbol.BaseType;
        while (baseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, baseType))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static bool IsOrInheritFrom(this ITypeSymbol symbol, ITypeSymbol expectedType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol, expectedType) ||
               (symbol is INamedTypeSymbol namedTypeSymbol && InheritsFrom(namedTypeSymbol, expectedType));
    }
    
    public static AttributeData? GetAttribute(this ISymbol symbol, ITypeSymbol attributeType, bool inherits = true)
    {
        if (attributeType.IsSealed)
        {
            inherits = false;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is null)
                continue;

            if (inherits)
            {
                if (attribute.AttributeClass.IsOrInheritFrom(attributeType))
                    return attribute;
            }
            else
            {
                if (SymbolEqualityComparer.Default.Equals(attributeType, attribute.AttributeClass))
                    return attribute;
            }
        }

        return null;
    }

    public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attributeType, bool inherits = true)
    {
        return GetAttribute(symbol, attributeType, inherits) is not null;
    }
    
    public static ITypeSymbol? GetUnderlyingNullableTypeOrSelf(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return null;
        
        if (namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T && namedTypeSymbol.TypeArguments.Length == 1)
        {
            return namedTypeSymbol.TypeArguments[0];
        }

        return null;
    }
    
    public static bool IsVisibleOutsideOfAssembly(this ISymbol symbol)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Public &&
            symbol.DeclaredAccessibility != Accessibility.Protected &&
            symbol.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
        {
            return false;
        }

        return symbol.ContainingType is null || IsVisibleOutsideOfAssembly(symbol.ContainingType);
    }
}