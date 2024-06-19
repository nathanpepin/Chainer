using Chainer.ChainServices;
using Chainer.Tests.FileContextChain;
using Chainer.Tests.FileContextChain.Handlers;
using FluentAssertions;

namespace Chainer.Tests;

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
        var result = await fileChain.ExecuteWithHistory(context);

        //Assert
        result.Result.IsSuccess.Should().Be(true);
        result.Handlers.Should().HaveCount(3);
        result.History.Should().HaveCount(3);
        result.UnappliedHandlers.Should().HaveCount(0);
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
        var result = await fileChain.ExecuteWithHistory(context);

        //Assert
        result.Result.IsSuccess.Should().Be(false);
        result.Handlers.Should().HaveCount(3);
        result.History.Should().HaveCount(historyCount);
        result.UnappliedHandlers.Should().HaveCount(notAppliedCount);
    }
}