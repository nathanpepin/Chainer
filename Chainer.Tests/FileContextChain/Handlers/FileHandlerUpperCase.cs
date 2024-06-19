using Chainer.ChainServices;
using CSharpFunctionalExtensions;

namespace Chainer.Tests.FileContextChain.Handlers;

public class FileHandlerUpperCase : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.ToUpperInvariant();
        return Task.FromResult<Result<FileContext>>(context);
    }
}