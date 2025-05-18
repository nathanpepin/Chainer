using Chainer.ChainServices;
using Chainer.Tests.ChainServices.ChainExecutor.FileContextChain.Handlers;
using Microsoft.Extensions.Logging;

namespace Chainer.Tests.ChainServices.ChainExecutor.FileContextChain.Chains;

public class FileChain(IServiceProvider services, ILogger<FileChain> logger) : ChainService<FileContext>(services, logger)
{
    protected override List<Type> ChainHandlers { get; } = [typeof(FileHandlerRemoveComma), typeof(FileHandlerUpperCase), typeof(FileHandlerIsLegit)];
}