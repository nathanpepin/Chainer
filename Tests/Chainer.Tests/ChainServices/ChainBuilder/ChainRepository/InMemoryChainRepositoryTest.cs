using Chainer.Building.Messages;
using Chainer.Building.Repository;
using FluentAssertions;
using JetBrains.Annotations;

namespace Chainer.Tests.ChainServices.ChainBuilder.ChainRepository;

[TestSubject(typeof(InMemoryChainRepository))]
public sealed class InMemoryChainRepositoryTest
{
    [Fact]
    public async Task GetChainMessagesAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();

        // Act
        var result = await repository.GetChainMessagesAsync(chainId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChainMessagesAsync_ThenGetChainMessagesAsync_ShouldReturnSavedMessages()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = "TestHandler",
                ExecutionOrder = 1,
                ContextTypeName = "TestContext"
            }
        };

        // Act
        var saveResult = await repository.SaveChainMessagesAsync(chainId, messages);
        var getResult = await repository.GetChainMessagesAsync(chainId);

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().HaveCount(1);
        getResult.Value.First().HandlerTypeName.Should().Be("TestHandler");
    }

    [Fact]
    public async Task SaveChainMessagesAsync_WithStringChainId_ShouldSaveAndRetrieveCorrectly()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = "test-chain-id";
        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(), // This will be overwritten when saved
                HandlerTypeName = "TestHandler",
                ExecutionOrder = 1,
                ContextTypeName = "TestContext"
            }
        };

        // Act
        var saveResult = await repository.SaveChainMessagesAsync(chainId, messages);
        var getResult = await repository.GetChainMessagesAsync(chainId);

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveChainExecutionLogs_ThenGetChainExecutionLogs_ShouldReturnSavedLogs()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var logs = new List<ChainExecutionLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = "TestHandler",
                ExecutionOrder = 1,
                Status = ChainMessageStatus.Completed
            }
        };

        // Act
        var saveResult = await repository.SaveChainExecutionLogs(logs);
        var getLogs = await repository.GetChainExecutionLogs(chainId);

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        getLogs.Should().HaveCount(1);
        getLogs.First().HandlerTypeName.Should().Be("TestHandler");
        getLogs.First().Status.Should().Be(ChainMessageStatus.Completed);
    }

    [Fact]
    public async Task UpdateChainExecutionLog_ShouldUpdateStatus()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var log = new ChainExecutionLog
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            HandlerTypeName = "TestHandler",
            ExecutionOrder = 1,
            Status = ChainMessageStatus.Pending
        };

        await repository.SaveChainExecutionLogs(new[] { log });

        // Act
        await repository.UpdateChainExecutionLog(log, ChainMessageStatus.Completed);
        var logs = await repository.GetChainExecutionLogs(chainId);

        // Assert
        logs.Should().HaveCount(1);
        logs.First().Status.Should().Be(ChainMessageStatus.Completed);
    }

    [Fact]
    public async Task UpdateChainExecutionLog_WithJsonData_ShouldUpdateStatusAndData()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var log = new ChainExecutionLog
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            HandlerTypeName = "TestHandler",
            ExecutionOrder = 1,
            Status = ChainMessageStatus.Pending
        };

        await repository.SaveChainExecutionLogs([log]);

        // Act
        await repository.UpdateChainExecutionLog(
            log,
            ChainMessageStatus.Completed,
            "{\"before\":\"data\"}",
            "{\"after\":\"data\"}");

        var logs = await repository.GetChainExecutionLogs(chainId);

        // Assert
        logs.Should().HaveCount(1);
        logs.First().Status.Should().Be(ChainMessageStatus.Completed);
        logs.First().BeforeJson.Should().Be("{\"before\":\"data\"}");
        logs.First().AfterJson.Should().Be("{\"after\":\"data\"}");
    }

    [Fact]
    public async Task DefaultChain_ShouldWorkWithDefaultGuid()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var messages = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = Guid.NewGuid(), // This will be overwritten when saved
                HandlerTypeName = "DefaultTestHandler",
                ExecutionOrder = 1,
                ContextTypeName = "TestContext"
            }
        };

        // Act
        var saveResult = await repository.SaveToDefaultChainMessagesAsync(messages);
        var getResult = await repository.GetDefaultChainMessagesAsync();

        // Assert
        saveResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().HaveCount(1);
        getResult.Value.First().HandlerTypeName.Should().Be("DefaultTestHandler");
        getResult.Value.First().ChainId.Should().Be(InMemoryChainRepository.DefaultChainGuid);
    }

    [Fact]
    public async Task SaveChainMessagesAsync_MultipleCalls_ShouldAppendMessages()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();

        var messages1 = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = "TestHandler1",
                ExecutionOrder = 1,
                ContextTypeName = "TestContext"
            }
        };

        var messages2 = new List<ChainMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = "TestHandler2",
                ExecutionOrder = 2,
                ContextTypeName = "TestContext"
            }
        };

        // Act
        await repository.SaveChainMessagesAsync(chainId, messages1);
        await repository.SaveChainMessagesAsync(chainId, messages2);
        var getResult = await repository.GetChainMessagesAsync(chainId);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().HaveCount(2);
        getResult.Value.Select(m => m.HandlerTypeName).Should().Contain(["TestHandler1", "TestHandler2"]);
    }

    [Fact]
    public async Task CancellationToken_WhenCancelled_ShouldReturnFailureResult()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await repository.GetChainMessagesAsync(chainId, cancellationTokenSource.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("canceled");
    }

    [Fact]
    public async Task UpdateChainExecutionLogs_ShouldReturnSuccess()
    {
        // Arrange
        var repository = new InMemoryChainRepository();
        var chainId = Guid.NewGuid();
        var logs = new List<ChainExecutionLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                HandlerTypeName = "TestHandler",
                ExecutionOrder = 1,
                Status = ChainMessageStatus.Pending
            }
        };

        await repository.SaveChainExecutionLogs(logs);

        // Act
        var result = await repository.UpdateChainExecutionLog(logs);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultChainExecutionLogs_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = new InMemoryChainRepository();

        // Act
        var logs = await repository.GetDefaultChainExecutionLogs();

        // Assert
        logs.Should().BeEmpty();
    }
}