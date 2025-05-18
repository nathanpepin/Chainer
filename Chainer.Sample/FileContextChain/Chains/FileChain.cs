using Chainer.ChainServices;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.FileContextChain.Chains;

public partial class FileChain(IServiceProvider services, ILogger<FileChain> logger)
    : ChainService<FileContext>(services, logger);