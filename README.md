# Chainer

## What is Chainer?

Chainer provides an abstraction that makes it easy to execute a series
of actions on a given context in sequence.
If an action fails internally, or fails during execution,
the error message is caught and reflected in the result.

The primary use case for the library is defining a series of processes
that should apply to some context with built-in error handling.

## Execution Options

Chainer offers two primary approaches to chain execution:

1. **Lightweight Chain Executor** - A simple, in-memory chain executor for straightforward sequential processing with
   minimal configuration. Ideal for direct application code where chains are defined at development time.

2. **Dynamic Chain Execution** - A more powerful, configurable system that can load chain definitions from external
   sources like databases or configuration files. Perfect for applications that need runtime chain configuration without
   code changes.

Choose the approach that best fits your needs - the lightweight executor for simplicity and direct control, or the
dynamic executor for flexibility and runtime configurability.

## Lightweight Chain Execution

The simplest chain is created using the ChainExecutor class.
First, make a context.

```csharp
public sealed class PriceContext : ICloneable
{
    public decimal InitialPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public Customer? Customer { get; init; }

    public object Clone()
    {
        return new PriceContext
        {
            InitialPrice = InitialPrice,
            CurrentPrice = CurrentPrice,
            Customer = Customer
        };
    }
}

public sealed class Customer
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool IsVip { get; init; }
}
```

Then create some handlers.

```csharp
public sealed class VipDiscount : IChainHandler<PriceContext>
{
    private const decimal VipDiscountAmount = 65;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Customer?.IsVip is not true) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice -= VipDiscountAmount;

        if (context.CurrentPrice < 0) context.CurrentPrice = 0;

        return Task.FromResult<Result<PriceContext>>(context);
    }
}

public sealed class OldAgeDiscount : IChainHandler<PriceContext>
{
    private const int MinimumAge = 65;
    private const decimal OldAgeDiscountAmount = 0.9m;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Customer?.Age is null or < MinimumAge) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice *= OldAgeDiscountAmount;

        return Task.FromResult<Result<PriceContext>>(context);
    }
}
```

Then define the chain using the method syntax.

```csharp
var chain = new ChainExecutor<PriceContext>()
            .AddHandler(new VipDiscount())
            .AddHandler(new OldAgeDiscount());
```

Or the constructor syntax.

```csharp
var chain = new ChainExecutor<PriceContext>([new VipDiscount(), new OldAgeDiscount()]);
```

Note that a logger can optionally be passed into the constructor.
The logger will be forwarded to the handlers.

## Execute

To execute, call:

```csharp
// Create a customer and pricing context
var customer = new Customer
{
    Name = "John Smith",
    Age = 70,
    IsVip = true
};

var context = new PriceContext 
{ 
    Customer = customer, 
    InitialPrice = 100,
    CurrentPrice = 100 
};

var result = await chain.Execute(context);

// The price will be reduced to 35 (100 - 65 VIP discount)
// Then to 31.5 (35 * 0.9 due to age discount)
Console.WriteLine($"Final price: {result.Value.CurrentPrice}"); // Output: Final price: 31.5
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
Context: Chainer.Sample.Pricing.PriceContext
Success: True
Error: None
Start: 2024-06-20T00:21:08
End: 2024-06-20T00:21:08
Execution Time: 0:00:00.0000505
Applied Handlers
        -Chainer.Sample.Pricing.Handlers.VipDiscount; Duration: 0:00:00.0000093
        -Chainer.Sample.Pricing.Handlers.OldAgeDiscount; Duration: 0:00:00.0000047
----------------------------------------
```

By default, the ExecuteWithHistory will clone and store the context at each step,
but if that isn't wanted, the method has a parameter to prevent that.

```csharp
public async Task<ContextHistoryResult<TContext>> ExecuteWithHistory(TContext? context,
        bool doNotCloneContext = false,
        CancellationToken cancellationToken = default)
```

## Using ChainBuilder

For more programmatic chain creation, you can use the ChainBuilder.

```csharp
var logger = loggerFactory.CreateLogger<PriceContext>();

var chain = new ChainBuilder<PriceContext>()
    .AddHandler(new VipDiscount())
    .AddHandler(new OldAgeDiscount())
    .WithLogger(logger)
    .TrackExecutionHistory() // Optional, for ExecuteWithHistory support
    .Build();
```

This approach provides a fluent interface for configuring chain options before building the executor.

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
public class PricingChain(IServiceProvider services, ILogger<PricingChain> logger) 
    : ChainService<PriceContext>(services, logger)
{
    //Can disable logging if wanted
    protected override bool LoggingEnabled => false;
    
    protected override List<Type> ChainHandlers { get; } = new List<Type>
    {
        typeof(VipDiscount), typeof(OldAgeDiscount), typeof(NonCustomerFee)
    };
}
```

If using the source generator the following case be used to
override the ChainHandlers and add all the types to the registration method.

```csharp
[RegisterChains<PriceContext>(
    typeof(VipDiscount),
    typeof(OldAgeDiscount),
    typeof(NonCustomerFee))]
public partial class PricingChain(IServiceProvider services, ILogger<PricingChain> logger) 
    : ChainService<PriceContext>(services, logger);
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
        services.TryAddScoped<PricingChain>();
        services.TryAddScoped<VipDiscount>();
        services.TryAddScoped<OldAgeDiscount>();
        services.TryAddScoped<NonCustomerFee>();
    }
}
```

## Dynamic Chain Configuration and Execution

Chainer now supports dynamic chain creation and execution through the `DynamicChainExecutor`. This allows you to define
chain configurations at runtime, store them (e.g., in a repository or configuration file), and execute them on demand.

The `DynamicChainExecutor` is designed with a database-centric model, where chain definitions can be stored in a
database and retrieved by name or ID at runtime. This architecture enables centralized chain management and makes it
possible to modify chain behavior without code changes.

### Loading Chain Configurations from Files

You can load chain configurations from settings files using the built-in integration:

### Using the Dynamic Chain Executor

```csharp
// Register services
builder.Services.AddScoped<IDynamicChainExecutor, DynamicChainExecutor>();

// Use the InMemoryChainRepository for development/testing
builder.Services.AddScoped<IChainRepository, InMemoryChainRepository>();
// OR use your custom database implementation
// builder.Services.AddScoped<IChainRepository, DatabaseChainRepository>();

// Configure chain handlers
builder.Services.AddScoped<FileHandlerRemoveComma>();
builder.Services.AddScoped<FileHandlerIsLegit>();
```

```csharp
// Add simple type mappings to resolve handler types
BindFromIConfiguration.AddSimpleTypeMaps<PriceContext>();
BindFromIConfiguration.AddSimpleTypeMaps<VipDiscount>();
BindFromIConfiguration.AddSimpleTypeMaps<OldAgeDiscount>();
BindFromIConfiguration.AddSimpleTypeMaps<NonCustomerFee>();

// Register chains from configuration
builder.Services.AddChainFromConfiguration(builder.Configuration, "PricingChain");
```

Example configuration in appsettings.json:

```json
{
  "PricingChain": {
    "ContextTypeName": "PriceContext",
    "Chains": [
      {
        "HandlerTypeName": "VipDiscount"
      },
      {
        "HandlerTypeName": "OldAgeDiscount"
      },
      {
        "HandlerTypeName": "NonCustomerFee",
        "Configuration": {
          "FeeAmount": 5.00
        }
      }
    ]
  }
}
```

### Executing Dynamic Chains

Once configured, you can execute chains by chain ID, friendly name, or directly with a list of chain messages:

```csharp
// Get the dynamic chain executor
var dynamicExecutor = host.Services.GetRequiredService<IDynamicChainExecutor>();

// Execute by chain name
var customer = new Customer
{
    Name = "Jane Doe",
    Age = 70,
    IsVip = true
};
var context = new PriceContext 
{ 
    Customer = customer, 
    InitialPrice = 100,
    CurrentPrice = 100 
};
var result = await dynamicExecutor.ExecuteChainAsync("PricingChain", context);

// Execute by chain ID
var result2 = await dynamicExecutor.ExecuteChainAsync(chainId, context);

// Execute default chain
var result3 = await dynamicExecutor.ExecuteDefaultChainAsync(context);
```

### Custom Repository Implementation

By default, Chainer provides an `InMemoryChainRepository` for development and testing, but for production use, you
should implement your own `IChainRepository` that connects to your database:

```csharp
public class DatabaseChainRepository : IChainRepository
{
    private readonly DbContext _dbContext;
    
    public DatabaseChainRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Result<List<ChainMessage>>> GetChainMessagesAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _dbContext.ChainMessages
                .Where(m => m.ChainId == chainId)
                .OrderBy(m => m.ExecutionOrder)
                .ToListAsync(cancellationToken);
                
            return Result<List<ChainMessage>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<List<ChainMessage>>.Failure(ex);
        }
    }
    
    public async Task<Result<List<ChainMessage>>> GetChainMessagesAsync(string friendlyName, CancellationToken cancellationToken = default)
    {
        // Create a deterministic GUID from the string name
        var chainId = GuidFromString.CreateDeterministicGuid(friendlyName);
        return await GetChainMessagesAsync(chainId, cancellationToken);
    }
    
    public async Task<Result<List<ChainMessage>>> GetDefaultChainMessagesAsync(CancellationToken cancellationToken = default)
    {
        // The default chain has a predefined GUID
        return await GetChainMessagesAsync(InMemoryChainRepository.DefaultChainGuid, cancellationToken);
    }
    
    // Implement other methods from IChainRepository interface...
}
```

Register your custom repository in your DI container:

```csharp
// Register your custom implementation
builder.Services.AddScoped<IChainRepository, DatabaseChainRepository>();

// Then register the dynamic executor that will use it
builder.Services.AddScoped<IDynamicChainExecutor, DynamicChainExecutor>();
```

With this approach, you can store chain definitions in your database and manage them through your application's
administrative interface or through database migrations.

### Configurable Handlers

You can create configurable handlers that accept configuration data from your chain definitions:

```csharp
public class ConfigurableDiscountHandler : IConfigurableChainHandler<PriceContext>
{
    private decimal _discountAmount = 10; // Default discount
    private bool _applyToAll = false;
    
    public void Configure(IHandlerConfiguration configuration)
    {
        var config = configuration.Bind<DiscountConfig>();
        if (config != null)
        {
            _discountAmount = config.DiscountAmount;
            _applyToAll = config.ApplyToAllCustomers;
        }
    }
    
    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, 
        CancellationToken cancellationToken = default)
    {
        // Apply discount conditionally based on configuration
        if (_applyToAll || context.Customer?.IsVip == true)
        {
            context.CurrentPrice -= _discountAmount;
            
            // Ensure price doesn't go below zero
            if (context.CurrentPrice < 0)
                context.CurrentPrice = 0;
        }
            
        return Task.FromResult<Result<PriceContext>>(context);
    }
    
    private class DiscountConfig
    {
        public decimal DiscountAmount { get; init; } = 10;
        public bool ApplyToAllCustomers { get; init; } = false;
    }
}
```

### Chain Execution History

The dynamic executor automatically tracks execution history. The result includes details about each handler's execution:

```csharp
var result = await dynamicExecutor.ExecuteChainAsync("FileProcessingChain", context);

foreach (var log in result.ExecutionLogs)
{
    Console.WriteLine($"{log.HandlerTypeName}: {log.Status} - {log.AfterJson}");
}
```

## Building Dynamic Chains Programmatically

You can also define chains programmatically using the `ChainDefinitionService`:

```csharp
var chainService = host.Services.GetRequiredService<IChainDefinitionService>();

var handlerCommands = new List<ChainCommand>
{
    new(typeof(VipDiscount), 
        new Dictionary<string, string>(), 
        HandlerConfigurationType.Dictionary, 
        0),
    new(typeof(OldAgeDiscount), 
        new { MinimumAge = 60, DiscountPercentage = 0.85 }, 
        HandlerConfigurationType.Object, 
        1)
};

var chainId = await chainService.CreateChainAsync<PriceContext>(handlerCommands);
```

This programmatic approach, combined with a database-backed repository, enables you to build administrative interfaces
where users can define and modify chains at runtime without code changes. This creates powerful flexibility for workflow
management, data processing pipelines, or any sequential operation that needs to be configurable.

## Future Plans

Though the library is intended to be limited is scope, feel free to give suggestions
or submit pull requests. The base functionality of the library is present,
but there could likely be improvements in testing and performance.