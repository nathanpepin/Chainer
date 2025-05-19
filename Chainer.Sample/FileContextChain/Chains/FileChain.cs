using Chainer.Core;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.FileContextChain.Chains;

public class FileChain(IServiceProvider services, ILogger<FileChain> logger)
    : ChainService<FileContext>(services, logger);