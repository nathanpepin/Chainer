using System.Threading.Tasks;
using ChainerGenerators;
using ConsoleApp1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Chainer.SourceGen.Sample;

public static class Program
{
    public static void Main()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices(services => { services.RegisterChains(); });

        var host = builder.Build();

        const string i = "My name,,,, is Nathan Pepin. and .I'm ";
        var context = new FileContext { Content = i };

        var chain = host.Services.GetRequiredService<FileChain>();
        var result = chain.ExecuteWithHistory(context).Result;
        ;
    }
}