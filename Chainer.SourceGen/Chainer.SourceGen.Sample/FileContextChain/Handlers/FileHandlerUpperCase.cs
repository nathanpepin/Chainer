using System.Threading;
using System.Threading.Tasks;
using Chainer.ChainServices;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Chainer.SourceGen.Sample.FileContextChain.Handlers;

public class FileHandlerUpperCase : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.ToUpperInvariant();
        return Task.FromResult<Result<FileContext>>(context);
    }
}