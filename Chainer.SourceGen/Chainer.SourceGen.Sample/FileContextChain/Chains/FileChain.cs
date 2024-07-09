using System;
using System.Collections.Generic;
using Chainer.ChainServices;
using Chainer.SourceGen.Sample.FileContextChain.Handlers;
using Microsoft.Extensions.Logging;

namespace Chainer.SourceGen.Sample.FileContextChain.Chains;

[RegisterChains<FileContext>(
    typeof(FileHandlerRemoveComma),
    typeof(FileHandlerIsLegit))]
public partial class FileChain(IServiceProvider services, ILogger<FileChain> logger)
    : ChainService<FileContext>(services, logger);