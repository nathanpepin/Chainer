using System.Diagnostics.CodeAnalysis;

namespace Chainer.Tests.ChainServices.ChainExecutor.FileContextChain;

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