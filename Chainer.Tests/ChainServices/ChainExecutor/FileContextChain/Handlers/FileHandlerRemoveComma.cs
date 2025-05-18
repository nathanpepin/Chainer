using Chainer.ChainServices;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Tests.ChainServices.ChainExecutor.FileContextChain.Handlers;

public class FileHandlerRemoveComma : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.Replace(",", "");
        return Task.FromResult<Result<FileContext>>(context);
    }
}