# Chainer

## What is Chainer?

Chainer provides an abstraction that makes it easy to execute a series
of actions on a given context in sequence.
If an action fails internally, or fails during execution,
the error message is caught and reflected in the result

The primary use case for the library is defining a series of processes
that should apply to some context with built-in error handling.

## How to define a chain

The simplest chain is can be created using the ChainExecutor class.
First, make a context.

```csharp
//Must be a class and implment IClonable and have a parameterless constructor
public class FileContext : ICloneable
{
    public string Content { get; set; } = "default";

    public object Clone()
    {
        return new FileContext
        {
            Content = Content
        };
    }
}
```

Then create some handlers.

```csharp
public class FileHandlerRemoveComma : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.Replace(",", "");
        return Task.FromResult<Result<FileContext>>(context);
    }
}

public class FileHandlerIsLegit : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        return !context.Content.Contains("Legit", StringComparison.InvariantCultureIgnoreCase)
            ? Task.FromResult(Result.Failure<FileContext>("This ain't legit"))
            : Task.FromResult<Result<FileContext>>(context);
    }
}
```
Then define the chain using the method syntax.

```csharp
var chain = new ChainExecutor<FileContext>()
            .AddHandler(new FileHandlerRemoveComma())
            .AddHandler(new FileHandlerIsLegit());
```
Or the constructor syntax.

```csharp
var chain = new ChainExecutor<FileContext>([new FileHandlerRemoveComma(), new FileHandlerIsLegit()]);
```

Note that a logger can optionally be passed into the constructor.
The logger will be forwarded to the handlers.

## Execute

To execute, call:

```csharp
var result = await chain.Execute(context);
```

If a null value is passed, the context will be newed up on execution.
This could be useful if one of the handler's populates context information.

## Execute With History

Or to execute with metadata about the execution:

```csharp
var result = await chain.ExecuteWithHistory(context);
Console.WriteLine(result);
```

Outputs

```text
----------------------------------------
Context: Chainer.SourceGen.Sample.FileContextChain.FileContext
Success: True
Error: None
Start: 2024-06-20T00:21:08
End: 2024-06-20T00:21:08
Execution Time: 0:00:00.0000505
Applied Handlers
        -Chainer.SourceGen.Sample.FileContextChain.Handlers.FileHandlerRemoveComma; Duration: 0:00:00.0000093
        -Chainer.SourceGen.Sample.FileContextChain.Handlers.FileHandlerIsLegit; Duration: 0:00:00.0000047
----------------------------------------
```

By default, the ExecuteWithHistory will clone and store the context at each step,
but if that isn't wanted, the method has an overload to prevent that.

```csharp
public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext? context,
        bool doNotCloneContext = false,
        CancellationToken cancellationToken = default)
```

## General Use

The ChainExecutor can be useful for chains that aren't predefined.
They can also have some use being defined as keyed services.

Handlers are not restricted in their scope and can be used for 
data import, export, validation, and so on.

## Chain Service

For predefined chains or chains that should be created via dependency injection,
a class can be defined that inherits from ChainService.
The executor will find the types specified from the DI container and execute the chain.

```csharp
public class FileChain(IServiceProvider services, ILogger<FileChain> logger) : ChainService<FileContext>(services, logger)
{
    //Can disable logging if wanted
    protected override bool LoggingEnabled => false;
    
    protected override List<Type> ChainHandlers { get; } = new List<Type>
    {
        typeof(FileHandlerRemoveComma), typeof(FileHandlerIsLegit)
    };
}
```

If using the source generator the following case be used to
override the ChainHandlers and add all the types to the registration method.

```csharp
[RegisterChains<FileContext>(
    typeof(FileHandlerRemoveComma),
    typeof(FileHandlerIsLegit))]
public partial class FileChain(IServiceProvider services, ILogger<FileChain> logger) : ChainService<FileContext>(services, logger);
```

Call the RegisterChains() method to register the services.

```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.RegisterChains();
var host = builder.Build();
```

The RegisterChains() will register all the services as follows.

```csharp
public static class ChainerRegistrar
{
    public static void RegisterChains(this IServiceCollection services)
    {
        services.TryAddScoped<FileChain>();
        services.TryAddScoped<FileHandlerRemoveComma>();
        services.TryAddScoped<FileHandlerIsLegit>();
    }
}
```

## Future Plans

Though the library is intended to be limited is scope, feel free to give suggestions
or submit pull requests. The base functionality of the library is present,
but there could likely be improvements in testing and performance.