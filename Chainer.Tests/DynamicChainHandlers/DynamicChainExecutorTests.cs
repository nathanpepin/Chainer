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
        result.Context.IsSuccess.Should().BeTrue();
        result.Context.Value.Value.Should().Be("Success");
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
        result.Context.IsFailure.Should().BeTrue();
        result.Context.Error.Should().Be("Deliberate failure");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithMultipleHandlers_ShouldExecuteInOrder()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var serviceProvider = A.Fake<IServiceProvider>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        var chainId = Guid.NewGuid();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = id3,
                ChainId = chainId,
                HandlerTypeName = typeof(TestSuccessHandler).AssemblyQualifiedName!,
                ExecutionOrder = 3,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = id1,
                ChainId = chainId,
                HandlerTypeName = typeof(TestFailureHandler).AssemblyQualifiedName!,
                ExecutionOrder = 1,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = id2,
                ChainId = chainId,
                HandlerTypeName = typeof(TestSuccessHandler).AssemblyQualifiedName!,
                ExecutionOrder = 2,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => serviceProvider.GetService(typeof(TestSuccessHandler))).Returns(new TestSuccessHandler());
        A.CallTo(() => serviceProvider.GetService(typeof(TestFailureHandler))).Returns(new TestFailureHandler());

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);

        // Act
        var result = await executor.ExecuteChainAsync(messages, new TestContext());

        // Assert
        result.Context.IsFailure.Should().BeTrue();
        result.Context.Error.Should().Be("Deliberate failure");

        // Verify logging to execution log
        result.ExecutionLogs.Should().HaveCount(3);

        result.ExecutionLogs[0].Id.Should().Be(id1);
        result.ExecutionLogs[1].Id.Should().Be(id2);
        result.ExecutionLogs[2].Id.Should().Be(id3);
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
        result.Context.IsSuccess.Should().BeTrue();
        result.Context.Value.Value.Should().Be("ConfiguredValue");
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
        result.Context.IsFailure.Should().BeTrue();
        result.Context.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithUnregisteredHandler_ShouldCreateHandlerViaActivator()
    {
        // Arrange
        var repository = A.Fake<IChainRepository>();
        var logger = A.Fake<ILogger<DynamicChainExecutor>>();

        // Create a real service provider with minimal services
        //Doing this because FakeItEasy has a hard time with null castings
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

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
        result.Context.IsSuccess.Should().BeTrue();
        result.Context.Value.Value.Should().Be("Success");
    }

    [Fact]
    public async Task ExecuteChainAsync_WithFailureInChain_ShouldSkipRemainingHandlers()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
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
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = typeof(TestFailureHandler).AssemblyQualifiedName!,
                ExecutionOrder = 2,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            },
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = typeof(TestSuccessHandler).AssemblyQualifiedName!,
                ExecutionOrder = 3,
                ContextTypeName = typeof(TestContext).AssemblyQualifiedName!
            }
        };

        A.CallTo(() => serviceProvider.GetService(typeof(TestSuccessHandler))).Returns(new TestSuccessHandler());
        A.CallTo(() => serviceProvider.GetService(typeof(TestFailureHandler))).Returns(new TestFailureHandler());

        var executor = new DynamicChainExecutor(repository, serviceProvider, logger);

        // Act
        var result = await executor.ExecuteChainAsync(messages, new TestContext());

        // Assert
        result.Context.IsFailure.Should().BeTrue();
        result.Context.Error.Should().Be("Deliberate failure");

        // Verify logging to execution log
        result.ExecutionLogs.Should().HaveCount(3);

        result.ExecutionLogs[0].HandlerTypeName.Should().Be(typeof(TestSuccessHandler).AssemblyQualifiedName!);
        result.ExecutionLogs[0].Status.Should().Be(ChainMessageStatus.Completed);

        result.ExecutionLogs[1].HandlerTypeName.Should().Be(typeof(TestFailureHandler).AssemblyQualifiedName!);
        result.ExecutionLogs[1].Status.Should().Be(ChainMessageStatus.Failed);

        result.ExecutionLogs[2].HandlerTypeName.Should().Be(typeof(TestSuccessHandler).AssemblyQualifiedName!);
        result.ExecutionLogs[2].Status.Should().Be(ChainMessageStatus.Skipped);
    }
}