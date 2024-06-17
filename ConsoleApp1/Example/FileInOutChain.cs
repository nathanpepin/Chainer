using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsoleApp1;

public class FileChain(IServiceProvider services) : ChainService<FileContext>(services)
{
    protected override List<Type> ChainHandlers { get; } =
        [typeof(FileHandlerUpperCase), typeof(FileHandlerRemoveComma), typeof(FileHandlerIsLegit)];
}

public class FileInOutChain(IServiceProvider services) : ChainInOutService<FileContext, string, string[]>(services)
{
    protected override List<Type> ChainHandlers { get; } =
        [typeof(FileHandlerUpperCase), typeof(FileHandlerRemoveComma), typeof(FileHandlerIsLegit)];

    protected override Func<string, Task<FileContext>> Import { get; } = text =>
    {
        var context = text.ToLower();
        return Task.FromResult(new FileContext { Content = context });
    };

    protected override Func<FileContext, Task<string[]>> Export { get; } =
        context => Task.FromResult(context.Content.Split(Environment.NewLine));
}

public class FileHandlerUpperCase : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.ToUpperInvariant();
        return Task.FromResult<Result<FileContext>>(context);
    }
}

public class FileHandlerRemoveComma : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.Replace(",", "");
        return Task.FromResult<Result<FileContext>>(context);
    }
}

public class FileHandlerIsLegit : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        return !context.Content.Contains("Legit", StringComparison.InvariantCultureIgnoreCase)
            ? Task.FromResult(Result.Failure<FileContext>("This ain't legit"))
            : Task.FromResult<Result<FileContext>>(context);
    }
}

public class FileContext : ICloneable
{
    [NotNull] public string? Content { get; set; }

    public object Clone()
    {
        return new FileContext
        {
            Content = Content
        };
    }
}