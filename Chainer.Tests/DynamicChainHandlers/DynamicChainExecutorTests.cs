using System.Text.Json;
using Chainer.ChainServices;
using Chainer.ChainServices.ChainBuilder.ChainRepository;
using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;
using Chainer.ChainServices.ChainBuilder.DynamicExecutors;
using Chainer.ChainServices.ChainBuilder.Messages;
using Chainer.Results;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chainer.Tests.DynamicChainHandlers;

public sealed class DynamicChainExecutorTests
{
    // Test context class
    private class TestContext : ICloneable
    {
        public string Value { get; set; } = "Initial";

        public object Clone()
        {
            return new TestContext { Value = Value };
        }
    }

// Test handler that succeeds
    private class TestSuccessHandler : IChainHandler<TestContext>
    {
        public Task<Result<TestContext>> Handle(TestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            context.Value = "Success";
            return Task.FromResult<Result<TestContext>>(context);
        }
    }

// Test handler that fails
    private class TestFailureHandler : IChainHandler<TestContext>
    {
        public Task<Result<TestContext>> Handle(TestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Failure<TestContext>("Deliberate failure"));
        }
    }

// Test configurable handler
    private class TestConfigurableHandler : IConfigurableChainHandler<TestContext>
    {
        private string _configValue = "Default";

        public void Configure(IHandlerConfiguration configuration)
        {
            var config = configuration.Bind<TestHandlerConfig>();
            if (config != null)
            {
                _configValue = config.ConfigValue;
            }
        }

        public Task<Result<TestContext>> Handle(TestContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            context.Value = _configValue;
            return Task.FromResult<Result<TestContext>>(context);
        }
    }

// Configuration class for the configurable handler
    private class TestHandlerConfig
    {
        public string ConfigValue { get; set; } = "Configured";
    }

    [Fact]
    public async Task ExecuteChainAsync_WhenGivenChainId_ShouldRetrieveMessagesAndExecuteChain()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        var chainId = Guid.NewGuid();
        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = typeof(TestSuccessHandler).AssemblyQualifiedName!,
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => repository.GetChainMessagesAsync(chainId, A<CancellationToken>._))
            .Returns(Result.Success(messages));

        A.CallTo(() => serviceProvider.GetService(typeof(TestSuccessHandler)))
            .Returns(new TestSuccessHandler());

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);
        var context = new TestContext();

        // Act
        var result = await executor.ExecuteChainAsync(chainId, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Success");
        A.CallTo(() => repository.GetChainMessagesAsync(chainId, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteChainAsync_WhenHandlerFails_ShouldReturnFailure()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = typeof(TestFailureHandler).AssemblyQualifiedName!,
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => serviceProvider.GetService(typeof(TestFailureHandler)))
            .Returns(new TestFailureHandler());

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);
        var context = new TestContext();

        // Act
        var result = await executor.ExecuteChainAsync(messages, context);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Deliberate failure");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithMultipleHandlers_ShouldExecuteInOrder()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        // Create a custom handler to track execution order
        var executionOrder = new List<int>();

        var handler1 = A.Fake<IChainHandler<TestContext>>();
        A.CallTo(() => handler1.Handle(A<TestContext>._, A<ILogger>._, A<CancellationToken>._))
            .Invokes(() => executionOrder.Add(1))
            .Returns(Result.Success(new TestContext { Value = "Handler1" }));

        var handler2 = A.Fake<IChainHandler<TestContext>>();
        A.CallTo(() => handler2.Handle(A<TestContext>._, A<ILogger>._, A<CancellationToken>._))
            .Invokes(() => executionOrder.Add(2))
            .Returns(Result.Success(new TestContext { Value = "Handler2" }));

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "Handler1",
                ExecutionOrder = 2, // Note: higher execution order but should run second
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "Handler2",
                ExecutionOrder = 1, // Lower execution order but should run first
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => serviceProvider.GetService(A<Type>.That.Matches(t => t.Name == "Handler1")))
            .Returns(handler1);
        A.CallTo(() => serviceProvider.GetService(A<Type>.That.Matches(t => t.Name == "Handler2")))
            .Returns(handler2);

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);

        // Act
        _ = await executor.ExecuteChainAsync(messages, new TestContext());

        // Assert
        executionOrder.Should().Equal(2, 1); // Handler2 (order 1) should execute before Handler1 (order 2)
    }

    [Fact]
    public async Task ExecuteChainAsync_WithConfigurableHandler_ShouldConfigureHandler()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        var config = new TestHandlerConfig { ConfigValue = "ConfiguredValue" };
        var configJson = JsonSerializer.Serialize(config);

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = typeof(TestConfigurableHandler).AssemblyQualifiedName!,
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!,
                ConfigurationJson = configJson
            }
        };

        A.CallTo(() => serviceProvider.GetService(typeof(TestConfigurableHandler)))
            .Returns(new TestConfigurableHandler());

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);
        var context = new TestContext();

        // Act
        var result = await executor.ExecuteChainAsync(messages, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ConfiguredValue");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithHandlerTypeNotFound_ShouldReturnFailure()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "NonExistentType",
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);
        var context = new TestContext();

        // Act
        var result = await executor.ExecuteChainAsync(messages, context);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithUnregisteredHandler_ShouldCreateHandlerViaActivator()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        // Return null from GetService to force creation via ActivatorUtilities
        A.CallTo(() => serviceProvider.GetService(typeof(TestSuccessHandler))).Returns(null);

        // Need to set up a scope factory for ActivatorUtilities
        var scopeFactory = A.Fake<IServiceScopeFactory>();
        var scope = A.Fake<IServiceScope>();
        var scopedProvider = A.Fake<IServiceProvider>();

        A.CallTo(() => serviceProvider.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory);
        A.CallTo(() => scopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(scopedProvider);

        // Return handler from scoped provider
        A.CallTo(() => scopedProvider.GetService(typeof(TestSuccessHandler))).Returns(new TestSuccessHandler());

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = typeof(TestSuccessHandler).AssemblyQualifiedName!,
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);
        var context = new TestContext();

        // Act
        var result = await executor.ExecuteChainAsync(messages, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Success");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithFailureInChain_ShouldSkipRemainingHandlers()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        // First handler succeeds
        var handler1 = new TestSuccessHandler();

        // Second handler fails
        var handler2 = new TestFailureHandler();

        // Third handler should be skipped
        var handler3 = A.Fake<IChainHandler<TestContext>>();

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "Handler1",
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "Handler2",
                ExecutionOrder = 2,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(),
                HandlerTypeName = "Handler3",
                ExecutionOrder = 3,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => serviceProvider.GetService(A<Type>.That.Matches(t => t.Name == "Handler1")))
            .Returns(handler1);
        A.CallTo(() => serviceProvider.GetService(A<Type>.That.Matches(t => t.Name == "Handler2")))
            .Returns(handler2);
        A.CallTo(() => serviceProvider.GetService(A<Type>.That.Matches(t => t.Name == "Handler3")))
            .Returns(handler3);

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);

        // Act
        var result = await executor.ExecuteChainAsync(messages, new TestContext());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Deliberate failure");

        // Make sure the third handler wasn't called
        A.CallTo(() => handler3.Handle(A<TestContext>._, A<ILogger>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        // Verify logging to execution log
        A.CallTo(() => repository.UpdateChainExecutionLog(
                A<ChainExecutionLog>.That.Matches(log => log.HandlerTypeName == "Handler3"),
                ChainMessageStatus.Skipped,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}