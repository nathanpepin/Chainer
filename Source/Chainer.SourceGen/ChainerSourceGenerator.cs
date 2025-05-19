using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using static Chainer.SourceGen.CodeText;


namespace Chainer.SourceGen;

[Generator]
public class ChainerSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            RegisterChainsAttributeFilename,
            SourceText.From(Helper.FormatCode(RegisterChainsAttribute), Encoding.UTF8)));

        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.attributeFound)
            .Select((t, _) => t.classDeclarationSyntax);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right));
    }

    private static (ClassDeclarationSyntax classDeclarationSyntax, bool attributeFound) GetClassDeclarationForSourceGen(
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

        foreach (var classDeclarationSyntax in classDeclarations)
        {
            var root = CSharpExtensions.GetCompilationUnitRoot(classDeclarationSyntax.SyntaxTree);

            var semanticModel = compilation.GetSemanticModel(root.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not ITypeSymbol classSymbol)
                continue;

            var classNamespace = classSymbol.ContainingNamespace.ToDisplayString();
            usings.Add($"using {classNamespace};");

            var classUsings = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(x => x.ToString())
                .Distinct()
                .ToImmutableArray();

            var attribute = classDeclarationSyntax
                .AttributeLists
                .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
                .First(x => x.Name.ToString().StartsWith(ChainRegistrationPrefix));

            var arguments = attribute.ArgumentList!.Arguments
                .Select(x => (TypeOfExpressionSyntax)x.Expression)
                .Select(x => x.Type.ToString())
                .ToImmutableArray();

            var typedArguments = string.Join(", ", arguments.Select(x => $"typeof({x})"));

            var registration = new Registration(classSymbol.Name);

            registration.Handlers.AddRange(arguments.Select(x => x.ToString()));

            foreach (var @using in classUsings)
                usings.Add(@using);

            registrations.Add(registration);

            var classImplementation = ChainServiceImpl(classUsings, classNamespace, classSymbol.Name, typedArguments);

            context.AddSource(GetChainHandlerFilename(classSymbol.Name), SourceText.From(Helper.FormatCode(classImplementation), Encoding.UTF8));
        }

        StringBuilder generatedCodeBuilder = new();
        foreach (var registration in registrations)
            registration.AddGenerateCode(generatedCodeBuilder);

        var code = GeneateChainServiceRegistrarCode(usings, generatedCodeBuilder);

        context.AddSource(ChainRegistrarFilename, SourceText.From(Helper.FormatCode(code), Encoding.UTF8));
    }
}