using Chainer.Abstractions;
using Chainer.Configuration;
using Chainer.Configuration.Binding;
using Chainer.Execution;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Chainer.Tests.TestUtilities;

/// <summary>
/// Provides common test fixtures and helper methods for Chainer tests.
/// </summary>
public static class TestFixtures
{
    #region Constants

    public const string DefaultChainName = "TestChain";
    public const string DefaultHandlerName = "TestHandler";
    public const int DefaultTimeout = 5000; // milliseconds

    public static readonly Guid TestChainId = Guid.Parse("12345678-1234-1234-1234-123456789012");
    public static readonly Guid TestMessageId = Guid.Parse("87654321-4321-4321-4321-210987654321");

    #endregion

    #region Context Factory Methods

    /// <summary>
    /// Creates a simple test context with default values.
    /// </summary>
    public static SimpleTestContext CreateSimpleContext(int id = 1, string name = "Test", decimal value = 100)
    {
        return new SimpleTestContext
        {
            Id = id,
            Name = name,
            Value = value,
            IsProcessed = false,
            ProcessedAt = null
        };
    }

    /// <summary>
    /// Creates a complex test context with sample order data.
    /// </summary>
    public static ComplexTestContext CreateComplexContext()
    {
        return new ComplexTestContext
        {
            TransactionId = Guid.NewGuid(),
            Order = new ComplexTestContext.OrderInfo
            {
                OrderId = 1001,
                Items = new List<ComplexTestContext.OrderItem>
                {
                    new() { ProductId = "P001", ProductName = "Widget", Quantity = 2, UnitPrice = 25.00m },
                    new() { ProductId = "P002", ProductName = "Gadget", Quantity = 1, UnitPrice = 50.00m }
                }
            },
            Customer = new ComplexTestContext.CustomerInfo
            {
                CustomerId = 501,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Type = ComplexTestContext.CustomerType.Premium,
                IsVip = true
            },
            Status = ComplexTestContext.ProcessingStatus.Pending
        };
    }

    /// <summary>
    /// Creates a tracking context for monitoring handler execution.
    /// </summary>
    public static TrackingTestContext CreateTrackingContext()
    {
        return new TrackingTestContext
        {
            Counter = 0,
            CurrentHandler = string.Empty,
            ShouldFailAtHandler = false,
            FailAtHandlerName = string.Empty
        };
    }

    /// <summary>
    /// Creates a validation context with various test scenarios.
    /// </summary>
    public static ValidationTestContext CreateValidationContext(bool valid = true)
    {
        if (valid)
        {
            return new ValidationTestContext
            {
                RequiredField = "Required Value",
                OptionalField = "Optional Value",
                Age = 25,
                Amount = 100.50m,
                Email = "test@example.com",
                PhoneNumber = "555-1234",
                IsValid = true
            };
        }

        return new ValidationTestContext
        {
            RequiredField = "", // Invalid
            Age = -5, // Invalid
            Amount = -100, // Invalid
            Email = "invalid-email", // Invalid
            IsValid = false
        };
    }

    #endregion

    #region Handler Factory Methods

    /// <summary>
    /// Creates a list of handlers that will execute successfully.
    /// </summary>
    public static List<IChainHandler<SimpleTestContext>> CreateSuccessfulHandlerChain(int count = 3)
    {
        var handlers = new List<IChainHandler<SimpleTestContext>>();

        for (int i = 0; i < count; i++)
        {
            handlers.Add(new IncrementHandler { IncrementBy = i + 1 });
        }

        return handlers;
    }

    /// <summary>
    /// Creates a handler chain with a failure at the specified position.
    /// </summary>
    public static List<IChainHandler<SimpleTestContext>> CreateFailingHandlerChain(int failAtPosition = 2, int totalHandlers = 5)
    {
        var handlers = new List<IChainHandler<SimpleTestContext>>();

        for (int i = 0; i < totalHandlers; i++)
        {
            if (i == failAtPosition)
            {
                handlers.Add(new AlwaysFailHandler { ErrorMessage = $"Failed at position {i}" });
            }
            else
            {
                handlers.Add(new IncrementHandler { IncrementBy = 1 });
            }
        }

        return handlers;
    }

    /// <summary>
    /// Creates tracking handlers for execution monitoring.
    /// </summary>
    public static List<IChainHandler<TrackingTestContext>> CreateTrackingHandlers(params string[] handlerNames)
    {
        return handlerNames
            .Select(name => new TrackingHandler { HandlerName = name })
            .Cast<IChainHandler<TrackingTestContext>>()
            .ToList();
    }

    #endregion

    #region ChainMessage Factory Methods

    /// <summary>
    /// Creates a basic chain message.
    /// </summary>
    public static ChainMessage CreateChainMessage(
        string handlerTypeName = null,
        int executionOrder = 0,
        string contextTypeName = null,
        object configuration = null)
    {
        return new ChainMessage
        {
            Id = Guid.NewGuid(),
            ChainId = TestChainId,
            FriendlyName = DefaultChainName,
            ExecutionOrder = executionOrder,
            HandlerTypeName = handlerTypeName ?? typeof(IncrementHandler).AssemblyQualifiedName!,
            ConfigurationType = configuration != null ? HandlerConfigurationType.Json : HandlerConfigurationType.NotSet,
            Configuration = configuration != null ? JsonSerializer.Serialize(configuration) : null,
            ContextTypeName = contextTypeName ?? typeof(SimpleTestContext).AssemblyQualifiedName!
        };
    }

    /// <summary>
    /// Creates a list of chain messages for a complete chain.
    /// </summary>
    public static List<ChainMessage> CreateChainMessages(params Type[] handlerTypes)
    {
        return handlerTypes
            .Select((type, index) => CreateChainMessage(
                handlerTypeName: type.AssemblyQualifiedName,
                executionOrder: index))
            .ToList();
    }

    /// <summary>
    /// Creates a chain configuration group for testing configuration binding.
    /// </summary>
    public static ChainConfigurationGroup CreateChainConfigurationGroup()
    {
        return new ChainConfigurationGroup
        {
            ContextTypeName = typeof(SimpleTestContext).AssemblyQualifiedName!,
            Chains = new List<ChainConfigurationItem>
            {
                new() { HandlerTypeName = nameof(IncrementHandler), Configuration = new { IncrementBy = 5 } },
                new() { HandlerTypeName = nameof(ProcessingHandler), Configuration = null },
                new() { HandlerTypeName = nameof(ConditionalFailHandler), Configuration = new { FailIfValueGreaterThan = 200 } }
            }
        };
    }

    #endregion

    #region Service Provider Factory Methods

    /// <summary>
    /// Creates a service provider with basic handler registrations.
    /// </summary>
    public static IServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices = null)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add basic handlers
        services.AddTransient<IncrementHandler>();
        services.AddTransient<ProcessingHandler>();
        services.AddTransient<AlwaysFailHandler>();
        services.AddTransient<ConfigurableMultiplierHandler>();

        // Allow custom configuration
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with a mock repository.
    /// </summary>
    public static IServiceProvider CreateServiceProviderWithRepository(IChainRepository repository = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(repository ?? A.Fake<IChainRepository>());
        services.AddTransient<DynamicChainExecutor>();

        return services.BuildServiceProvider();
    }

    #endregion

    #region Mock Factory Methods

    /// <summary>
    /// Creates a mock logger.
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return A.Fake<ILogger<T>>();
    }

    /// <summary>
    /// Creates a mock handler configuration.
    /// </summary>
    public static IHandlerConfiguration CreateMockConfiguration<T>(T configObject) where T : class, new()
    {
        var config = A.Fake<IHandlerConfiguration>();
        A.CallTo(() => config.ConfigurationType).Returns(HandlerConfigurationType.Object);
        A.CallTo(() => config.Bind<T>()).Returns(configObject);
        A.CallTo(() => config.TryBind<T>(out configObject)).Returns(true);
        return config;
    }

    /// <summary>
    /// Creates a mock chain repository with test data.
    /// </summary>
    public static IChainRepository CreateMockRepository(List<ChainMessage> messages = null)
    {
        var repository = A.Fake<IChainRepository>();
        messages ??= CreateChainMessages(typeof(IncrementHandler), typeof(ProcessingHandler));

        A.CallTo(() => repository.GetChainMessagesAsync(A<Guid>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<List<ChainMessage>>.Success(messages)));

        A.CallTo(() => repository.GetChainMessagesAsync(A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result<List<ChainMessage>>.Success(messages)));

        A.CallTo(() => repository.SaveChainMessagesAsync(A<Guid>._, A<IEnumerable<ChainMessage>>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Success()));

        A.CallTo(() => repository.SaveChainExecutionLogs(A<IEnumerable<ChainExecutionLog>>._, A<CancellationToken>._))
            .Returns(Task.FromResult(Result.Success()));

        return repository;
    }

    #endregion

    #region Configuration Builders

    /// <summary>
    /// Creates an IConfiguration with test chain configuration.
    /// </summary>
    public static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            ["TestChain:ContextTypeName"] = "SimpleTestContext",
            ["TestChain:Chains:0:HandlerTypeName"] = "IncrementHandler",
            ["TestChain:Chains:0:Configuration:IncrementBy"] = "10",
            ["TestChain:Chains:1:HandlerTypeName"] = "ProcessingHandler"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    /// <summary>
    /// Creates handler configuration from various sources.
    /// </summary>
    public static ChainHandlerConfiguration CreateHandlerConfiguration<T>(T configObject) where T : class
    {
        var json = JsonSerializer.Serialize(configObject);
        return new ChainHandlerConfiguration(json, HandlerConfigurationType.Json);
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    /// Asserts that a chain execution result is successful with expected context state.
    /// </summary>
    public static void AssertSuccessfulExecution<TContext>(
        ChainExecutionResult<TContext> result,
        Action<TContext> contextAssertion = null)
        where TContext : class, ICloneable, new()
    {
        result.IsSuccess.ShouldBeTrue();
        result.Context.IsSuccess.ShouldBeTrue();
        contextAssertion?.Invoke(result.Context.Value);
    }

    /// <summary>
    /// Asserts that a chain execution failed with expected error.
    /// </summary>
    public static void AssertFailedExecution<TContext>(
        ChainExecutionResult<TContext> result,
        string expectedError = null)
        where TContext : class, ICloneable, new()
    {
        result.IsFailure.ShouldBeTrue();
        result.Context.IsFailure.ShouldBeTrue();

        if (expectedError != null)
        {
            result.Context.Error.ShouldContain(expectedError);
        }
    }

    /// <summary>
    /// Asserts execution log states.
    /// </summary>
    public static void AssertExecutionLogs(
        IEnumerable<ChainExecutionLog> logs,
        params ChainMessageStatus[] expectedStatuses)
    {
        var logList = logs.ToList();
        logList.Count.ShouldBe(expectedStatuses.Length);

        for (int i = 0; i < expectedStatuses.Length; i++)
        {
            logList[i].Status.ShouldBe(expectedStatuses[i]);
        }
    }

    #endregion

    #region Test Data Generators

    /// <summary>
    /// Generates test contexts with varying data.
    /// </summary>
    public static IEnumerable<SimpleTestContext> GenerateTestContexts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new SimpleTestContext
            {
                Id = i + 1,
                Name = $"Test_{i}",
                Value = (i + 1) * 10,
                IsProcessed = i % 2 == 0
            };
        }
    }

    /// <summary>
    /// Creates a cancellation token that cancels after a delay.
    /// </summary>
    public static CancellationToken CreateCancellationToken(int delayMilliseconds)
    {
        var cts = new CancellationTokenSource(delayMilliseconds);
        return cts.Token;
    }

    #endregion
}