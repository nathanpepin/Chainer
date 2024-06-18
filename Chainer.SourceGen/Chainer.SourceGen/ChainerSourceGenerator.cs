using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;


namespace Chainer.SourceGen;

public static class Regexes
{
    public static Regex TypeName { get; } = new("<(.*)>");
}

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class ChainerSourceGenerator : IIncrementalGenerator
{
    private const string Namespace = "Chainer.ChainServices";
    private const string AttributeName = "RegisterChainsAttribute";
    private const string AttributeFullPath = $"{Namespace}.{AttributeName}";
    private const string ChainRegistrationPrefix = "RegisterChains<";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.attributeFound)
            .Select((t, _) => t.Item1);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    private static (ClassDeclarationSyntax, bool attributeFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        var attribute = classDeclarationSyntax
            .AttributeLists
            .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
            .FirstOrDefault(x => x.Name.ToString().StartsWith(ChainRegistrationPrefix));

        return attribute is null
            ? (classDeclarationSyntax, false)
            : (classDeclarationSyntax, true);
    }
    
    private static void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        List<Registration> registrations = [];
        HashSet<string> usings = [];

        SyntaxNode? root = null;

        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var compilationRoot = CSharpExtensions.GetCompilationUnitRoot(classDeclarationSyntax.SyntaxTree);

            root ??= classDeclarationSyntax.SyntaxTree.GetRoot();

            var semanticModel = compilation.GetSemanticModel(compilationRoot.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not ITypeSymbol classSymbol)
                continue;
            
            var className = classSymbol.Name;

            var classNamespace = classSymbol.ContainingNamespace.ToDisplayString();
            usings.Add($"using {classNamespace};");

            var usingsArray = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(x => x.ToString())
                .ToImmutableArray();

            var attribute = classDeclarationSyntax
                .AttributeLists
                .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
                .First(x => x.Name.ToString().StartsWith(ChainRegistrationPrefix));

            var j = classSymbol.DeclaringSyntaxReferences;
            var d = classSymbol.GetAttributes();
            var k = classSymbol.GetTypeMembers();
            var p = classSymbol.GetType();

           var kk =  semanticModel.Compilation.GetTypeByMetadataName("Chainer.ChainServices.");
            
            var contextName = GetContextName(attribute.Name.ToString().AsSpan());

            var typeArguments = attribute.ArgumentList!.Arguments.ToString();

            var arguments = attribute.ArgumentList!.Arguments
                .Select(x => (TypeOfExpressionSyntax)x.Expression)
                .Select(x => x.Type.ToString())
                .ToImmutableArray();

            var registration = new Registration(className, contextName);

            registration.Handlers.AddRange(arguments.Select(x => x.ToString()));

            foreach (var @using in usingsArray)
                usings.Add(@using);

            registrations.Add(registration);

             var classImplementation =
                 $$"""
                   using System.Collections.Generic;
                   {{string.Join("\r\n", usingsArray)}}

                   namespace {{classNamespace}}
                   {
                       partial class {{className}}
                       {
                           protected override List<Type> ChainHandlers { get; } = new List<Type> { {{typeArguments}} };
                       }
                   }
                   """;
             
             context.AddSource($"{className}-ChainHandlers.g.cs", SourceText.From(Helper.FormatCode(classImplementation), Encoding.UTF8));
        }

        StringBuilder generatedCodeBuilder = new();

        foreach (var registration in registrations)
            registration.AddGenerateCode(generatedCodeBuilder);

        var code =
            $$"""
              // <auto-generated/>

              using System;
              using System.Collections.Generic;
              using Microsoft.Extensions.DependencyInjection;
              using Microsoft.Extensions.DependencyInjection.Extensions;
              using Microsoft.Extensions.Hosting;
              {{string.Join("\r\n", usings)}}

              namespace {{Namespace}}
              {
                  public static class ChainerRegistrar
                  {
                      public static void RegisterChains(this IServiceCollection services)
                      {
                          {{generatedCodeBuilder}}
                      }
                  }
              }
              """;

        context.AddSource("ChainerRegistrar.g.cs", SourceText.From(Helper.FormatCode(code), Encoding.UTF8));
    }
    
    private static string GetContextName(ReadOnlySpan<char> attributeContextName)
    {
        var start = attributeContextName.IndexOf('<');
        var end = attributeContextName.IndexOf('>');
        var length = end - start;
        var slice = attributeContextName.Slice(start + 1, length - 1);
        return slice.ToString();
    }

}