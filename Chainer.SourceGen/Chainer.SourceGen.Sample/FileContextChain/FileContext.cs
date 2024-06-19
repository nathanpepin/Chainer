using System;
using System.Diagnostics.CodeAnalysis;

namespace Chainer.SourceGen.Sample.FileContextChain;

public class FileContext : ICloneable
{
    [NotNull] public string? Content { get; set; } = "default";
    
    public object Clone()
    {
        return new FileContext
        {
            Content = Content
        };
    }
}