using System;
using System.Threading;
using System.Threading.Tasks;
using Chainer.ChainServices;
using CSharpFunctionalExtensions;

namespace Chainer.SourceGen.Sample.FileContextChain.Handlers;

public class FileHandlerIsLegit : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        return !context.Content.Contains("Legit", StringComparison.InvariantCultureIgnoreCase)
            ? Task.FromResult(Result.Failure<FileContext>("This ain't legit"))
            : Task.FromResult<Result<FileContext>>(context);
    }
}