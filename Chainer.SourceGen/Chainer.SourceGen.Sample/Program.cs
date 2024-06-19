using System;
using Chainer.ChainServices;
using Chainer.SourceGen.Sample.FileContextChain;
using Chainer.SourceGen.Sample.FileContextChain.Chains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chainer.SourceGen.Sample;

public static class Program
{
    public static void Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.RegisterChains();

        var host = builder.Build();

        const string i = "My name,,,, is Nathan Pepin. and .I'm legit";
        var context = new FileContext { Content = i };

        var chain = host.Services.GetRequiredService<FileChain>();
        var output = chain.ExecuteWithHistory(context).Result;

        Console.WriteLine(output);
    }
}