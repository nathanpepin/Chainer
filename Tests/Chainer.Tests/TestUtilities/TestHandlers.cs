using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Persistence;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Tests.TestUtilities;

#region Basic Handlers

/// <summary>
/// A handler that always succeeds and increments a counter.
/// </summary>
public sealed class IncrementHandler : IChainHandler<SimpleTestContext>
{
    public int IncrementBy { get; init; } = 1;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Incrementing value by {IncrementBy}", IncrementBy);
        context.Value += IncrementBy;
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

/// <summary>
/// A handler that always fails with a specific error message.
/// </summary>
public sealed class AlwaysFailHandler : IChainHandler<SimpleTestContext>
{
    public string ErrorMessage { get; init; } = "Handler failed as expected";

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogError("Handler failing with message: {ErrorMessage}", ErrorMessage);
        return Task.FromResult(Result<SimpleTestContext>.Failure(ErrorMessage));
    }
}

/// <summary>
/// A handler that throws an exception.
/// </summary>
public sealed class ThrowingHandler : IChainHandler<SimpleTestContext>
{
    public string ExceptionMessage { get; init; } = "Handler threw exception";

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(ExceptionMessage);
    }
}

/// <summary>
/// A handler that marks the context as processed.
/// </summary>
public sealed class ProcessingHandler : IChainHandler<SimpleTestContext>
{
    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.IsProcessed = true;
        context.ProcessedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

#endregion

#region Conditional Handlers

/// <summary>
/// A handler that fails based on a condition in the context.
/// </summary>
public sealed class ConditionalFailHandler : IChainHandler<SimpleTestContext>
{
    public decimal FailIfValueGreaterThan { get; init; } = 100;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Value > FailIfValueGreaterThan)
        {
            return Task.FromResult(Result<SimpleTestContext>.Failure($"Value {context.Value} exceeds maximum {FailIfValueGreaterThan}"));
        }

        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

/// <summary>
/// A handler that only processes if a condition is met.
/// </summary>
public sealed class ConditionalProcessHandler : IChainHandler<SimpleTestContext>
{
    public bool RequireProcessed { get; init; } = false;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (RequireProcessed && !context.IsProcessed)
        {
            return Task.FromResult(Result<SimpleTestContext>.Failure("Context must be processed before this handler"));
        }

        context.Name = $"{context.Name}_Processed";
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

#endregion

#region Configurable Handlers

/// <summary>
/// A configurable handler that multiplies the value by a configured amount.
/// </summary>
public sealed class ConfigurableMultiplierHandler : IConfigurableChainHandler<SimpleTestContext>
{
    private MultiplierConfig _config = new();

    public void Configure(IHandlerConfiguration configuration)
    {
        _config = configuration.Bind<MultiplierConfig>() ?? new MultiplierConfig();
    }

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Multiplying value by {Multiplier}", _config.Multiplier);
        context.Value *= _config.Multiplier;

        if (_config.SetProcessed)
        {
            context.IsProcessed = true;
        }

        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }

    public class MultiplierConfig
    {
        public decimal Multiplier { get; set; } = 1.0m;
        public bool SetProcessed { get; set; } = false;
    }
}

/// <summary>
/// A handler that uses configuration to determine behavior.
/// </summary>
public sealed class ConfigurableBehaviorHandler : HandlerConfiguration<ConfigurableTestContext, ConfigurableBehaviorHandler.BehaviorConfig>
{
    public override Task<Result<ConfigurableTestContext>> Handle(ConfigurableTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (Configuration?.FailOnFeatureA == true && context.EnableFeatureA)
        {
            return Task.FromResult(Result<ConfigurableTestContext>.Failure("Feature A is not allowed"));
        }

        if (Configuration?.RequireFeatureB == true && !context.EnableFeatureB)
        {
            return Task.FromResult(Result<ConfigurableTestContext>.Failure("Feature B is required"));
        }

        if (Configuration?.TransformOutput == true)
        {
            context.OutputValue = $"{Configuration.OutputPrefix}{context.InputValue}{Configuration.OutputSuffix}";
        }

        return Task.FromResult(Result<ConfigurableTestContext>.Success(context));
    }

    public class BehaviorConfig
    {
        public bool FailOnFeatureA { get; set; }
        public bool RequireFeatureB { get; set; }
        public bool TransformOutput { get; set; }
        public string OutputPrefix { get; set; } = "";
        public string OutputSuffix { get; set; } = "";
    }
}

#endregion

#region Persistence Handlers

/// <summary>
/// A handler that saves context before execution.
/// </summary>
public sealed class PrePersistenceHandler : IChainHandler<SimpleTestContext>, IContextPersistence
{
    public PersistencePoint PersistWhen => PersistencePoint.BeforeExecution;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.Value *= 2;
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

/// <summary>
/// A handler that saves context after execution.
/// </summary>
public sealed class PostPersistenceHandler : IChainHandler<SimpleTestContext>, IContextPersistence
{
    public PersistencePoint PersistWhen => PersistencePoint.AfterExecution;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.Value += 10;
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

/// <summary>
/// A handler that saves context both before and after execution.
/// </summary>
public sealed class BothPersistenceHandler : IChainHandler<SimpleTestContext>, IContextPersistence
{
    public PersistencePoint PersistWhen => PersistencePoint.Both;

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var oldValue = context.Value;
        context.Value = oldValue * oldValue; // Square the value
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

#endregion

#region Async and Cancellation Handlers

/// <summary>
/// A handler that performs async operations and respects cancellation.
/// </summary>
public sealed class AsyncDelayHandler : IChainHandler<SimpleTestContext>
{
    public int DelayMilliseconds { get; init; } = 100;

    public async Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Starting async delay of {Delay}ms", DelayMilliseconds);

        try
        {
            await Task.Delay(DelayMilliseconds, cancellationToken);
            context.Name = $"{context.Name}_Delayed";
            return Result<SimpleTestContext>.Success(context);
        }
        catch (OperationCanceledException)
        {
            logger?.LogWarning("Operation was cancelled");
            return Result<SimpleTestContext>.Failure("Operation was cancelled");
        }
    }
}

/// <summary>
/// A handler that checks for cancellation before processing.
/// </summary>
public sealed class CancellationAwareHandler : IChainHandler<SimpleTestContext>
{
    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(Result<SimpleTestContext>.Failure("Cancellation was requested"));
        }

        context.Name = $"{context.Name}_NotCancelled";
        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

#endregion

#region Tracking Handlers

/// <summary>
/// A handler that tracks its execution in the context.
/// </summary>
public sealed class TrackingHandler : IChainHandler<TrackingTestContext>
{
    public string HandlerName { get; init; } = "TrackingHandler";

    public Task<Result<TrackingTestContext>> Handle(TrackingTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.CurrentHandler = HandlerName;
        context.AddModification($"Handler '{HandlerName}' executed");
        context.IncrementHandlerCount(HandlerName);
        context.Counter++;

        if (context.ShouldFailAtHandler && context.FailAtHandlerName == HandlerName)
        {
            return Task.FromResult(Result<TrackingTestContext>.Failure($"Failed at handler: {HandlerName}"));
        }

        return Task.FromResult(Result<TrackingTestContext>.Success(context));
    }
}

/// <summary>
/// A handler that logs detailed information.
/// </summary>
public sealed class LoggingHandler : IChainHandler<SimpleTestContext>
{
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
    public string LogMessage { get; init; } = "Handler executed";

    public Task<Result<SimpleTestContext>> Handle(SimpleTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger?.Log(LogLevel, "Context state - Id: {Id}, Name: {Name}, Value: {Value}",
            context.Id, context.Name, context.Value);
        logger?.Log(LogLevel, LogMessage);

        return Task.FromResult(Result<SimpleTestContext>.Success(context));
    }
}

#endregion

#region Complex Scenario Handlers

/// <summary>
/// A handler for processing orders in complex contexts.
/// </summary>
public sealed class OrderProcessingHandler : IChainHandler<ComplexTestContext>
{
    public Task<Result<ComplexTestContext>> Handle(ComplexTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.ProcessingSteps.Add("OrderProcessing");

        var total = context.Order.Items.Sum(i => i.Quantity * i.UnitPrice);
        context.Order.TotalAmount = total;

        // Apply VIP discount
        if (context.Customer.IsVip)
        {
            context.Order.DiscountAmount = total * 0.1m; // 10% discount
        }

        context.Order.FinalAmount = total - context.Order.DiscountAmount;
        context.Status = ComplexTestContext.ProcessingStatus.Processing;

        return Task.FromResult(Result<ComplexTestContext>.Success(context));
    }
}

/// <summary>
/// A handler for validating complex contexts.
/// </summary>
public sealed class ValidationHandler : IChainHandler<ValidationTestContext>
{
    public Task<Result<ValidationTestContext>> Handle(ValidationTestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.ValidationErrors.Clear();

        if (string.IsNullOrWhiteSpace(context.RequiredField))
        {
            context.ValidationErrors.Add("RequiredField is required");
        }

        if (context.Age < 0 || context.Age > 150)
        {
            context.ValidationErrors.Add("Age must be between 0 and 150");
        }

        if (context.Amount < 0)
        {
            context.ValidationErrors.Add("Amount cannot be negative");
        }

        if (!string.IsNullOrEmpty(context.Email) && !context.Email.Contains('@'))
        {
            context.ValidationErrors.Add("Email format is invalid");
        }

        context.IsValid = context.ValidationErrors.Count == 0;

        if (!context.IsValid)
        {
            return Task.FromResult(Result<ValidationTestContext>.Failure($"Validation failed: {string.Join(", ", context.ValidationErrors)}"));
        }

        return Task.FromResult(Result<ValidationTestContext>.Success(context));
    }
}

#endregion

#region Generic Test Handler

/// <summary>
/// A generic handler that can be configured for various test scenarios.
/// </summary>
public sealed class GenericTestHandler<TContext> : IChainHandler<TContext>
    where TContext : class, ICloneable, new()
{
    public Func<TContext, ILogger?, CancellationToken, Task<Result<TContext>>>? HandlerFunc { get; init; }

    public Task<Result<TContext>> Handle(TContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (HandlerFunc != null)
        {
            return HandlerFunc(context, logger, cancellationToken);
        }

        return Task.FromResult(Result<TContext>.Success(context));
    }
}

#endregion