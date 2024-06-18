using System.Threading;
using System.Threading.Tasks;
using Chainer.ChainServices;
using CSharpFunctionalExtensions;

namespace Chainer.SourceGen.Sample.FileContextChain.Handlers;

public class FileHandlerRemoveComma : IChainHandler<FileContext>
{
    public Task<Result<FileContext>> Handle(FileContext context, CancellationToken cancellationToken = default)
    {
        context.Content = context.Content.Replace(",", "");
        return Task.FromResult<Result<FileContext>>(context);
    }
}