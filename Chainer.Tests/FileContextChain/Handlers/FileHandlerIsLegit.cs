using Chainer.ChainServices;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Chainer.Tests.FileContextChain.Handlers;

public class FileHandlerIsLegit : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        return !context.Content.Contains("Legit", StringComparison.InvariantCultureIgnoreCase)
            ? Task.FromResult(Result.Failure<FileContext>("This ain't legit"))
            : Task.FromResult<Result<FileContext>>(context);
    }
}