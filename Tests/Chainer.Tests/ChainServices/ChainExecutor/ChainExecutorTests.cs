using Chainer.Building.Messages;
using Chainer.Core;
using Chainer.Tests.ChainServices.ChainExecutor.FileContextChain;
using Chainer.Tests.ChainServices.ChainExecutor.FileContextChain.Handlers;
using FluentAssertions;

namespace Chainer.Tests.ChainServices.ChainExecutor;

public class ChainExecutorTests
{
    private static readonly ChainExecutor<FileContext> TestFileChain =
        new ChainExecutor<FileContext>()
            .AddHandler(new FileHandlerUpperCase())
            .AddHandler(new FileHandlerRemoveComma())
            .AddHandler(new FileHandlerIsLegit());

    [Theory]
    [InlineData("My name,,,, is Nathan Pepin. and .I'm legit", "MY NAME IS NATHAN PEPIN. AND .I'M LEGIT")]
    public async Task ChainExecutor_Execute_ShouldBeSuccess(string input, string expectedOutput)
    {
        //Arrange
        var fileChain = TestFileChain;
        var context = new FileContext { Content = input };

        //Act
        var result = await fileChain.Execute(context);

        //Assert
        result.IsSuccess.Should().Be(true);
        context.Content.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("My name,,,, is Nathan Pepin. and .I'm l")]
    [InlineData(null!)]
    [InlineData("")]
    public async Task ChainExecutor_Execute_ShouldBeFailure(string input)
    {
        //Arrange
        var fileChain = TestFileChain;
        var context = new FileContext { Content = input };

        //Act
        var result = await fileChain.Execute(context);

        //Assert
        result.IsSuccess.Should().Be(false);
    }

    [Theory]
    [InlineData("My name,,,, is Nathan Pepin. and .I'm legit", "MY NAME IS NATHAN PEPIN. AND .I'M LEGIT")]
    public async Task ChainExecutor_ExecuteWithHistory_ShouldBeSuccess(string input, string expectedOutput)
    {
        //Arrange
        var fileChain = TestFileChain;
        var context = new FileContext { Content = input };

        //Act
        var result = await fileChain.Execute(context);

        //Assert
        result.Context.IsSuccess.Should().Be(true);
        result.ExecutionLogs.Should().HaveCount(3);
        result.ExecutionLogs.Select(x => x.Status == ChainMessageStatus.Completed).Should().HaveCount(3);
        context.Content.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("My name,,,, is Nathan Pepin. and .I'm l", 2, 1)]
    [InlineData(null!, 0, 3)]
    [InlineData("", 2, 1)]
    public async Task ChainExecutor_ExecuteWithHistory_ShouldBeFailure(string input, int historyCount, int notAppliedCount)
    {
        //Arrange
        var fileChain = TestFileChain;
        var context = new FileContext { Content = input };

        //Act
        var result = await fileChain.Execute(context);

        //Assert
        result.Context.IsSuccess.Should().Be(false);
        result.ExecutionLogs.Should().HaveCount(3);
        result.ExecutionLogs.Select(x => x.Status == ChainMessageStatus.Completed).Should().HaveCount(notAppliedCount);
    }
}