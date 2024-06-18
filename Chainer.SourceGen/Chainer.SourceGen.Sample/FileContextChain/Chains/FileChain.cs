using System;
using Chainer.ChainServices;
using Chainer.SourceGen.Sample.FileContextChain.Handlers;

namespace Chainer.SourceGen.Sample.FileContextChain.Chains;

[RegisterChains<FileContext>(
    typeof(FileHandlerUpperCase),
    typeof(FileHandlerRemoveComma),
    typeof(FileHandlerIsLegit))]
public partial class FileChain(IServiceProvider services) : ChainService<FileContext>(services);