using System;
using System.Threading;
using System.Threading.Tasks;
using Chainer.Core;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.SourceGen.Sample.FileContextChain.Handlers;

public class FileHandlerIsLegit : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        return !context.Content.Contains("Legit", StringComparison.InvariantCultureIgnoreCase)
            ? Task.FromResult(Result<FileContext>.Failure("This ain't legit"))
            : Task.FromResult<Result<FileContext>>(context);
    }
}