using System;
using System.Collections.Generic;
using ConsoleApp1;

namespace Chainer.SourceGen.Sample;

[RegisterChains<FileContext>(
    typeof(FileHandlerUpperCase),
    typeof(FileHandlerRemoveComma),
    typeof(FileHandlerIsLegit))]
public partial class FileChain(IServiceProvider services) : ChainService<FileContext>(services);