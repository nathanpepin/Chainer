using Chainer.ChainServices;
using Chainer.Tests.FileContextChain.Handlers;

namespace Chainer.Tests.FileContextChain.Chains;

public class FileChain(IServiceProvider services) : ChainService<FileContext>(services)
{
    protected override List<Type> ChainHandlers { get; } = [typeof(FileHandlerRemoveComma), typeof(FileHandlerUpperCase), typeof(FileHandlerIsLegit)];
}