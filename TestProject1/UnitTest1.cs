

using ConsoleApp1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices(services => { services.RegisterChains(); });

        var h = builder.Build();

        const string i = "My name,,,, is Nathan Pepin. and .I'm legit";
        var context = new FileContext { Content = i };

        var chain = h.Services.GetRequiredService<FileChain>();
        var result = await chain.ExecuteWithHistory(context);
    }
}

public ref struct A
{
    public ReadOnlySpan<int> Value { get; set; }
}

[RegisterChains<FileContext>(typeof(FileHandlerUpperCase), typeof(FileHandlerRemoveComma), typeof(FileHandlerIsLegit))]
public partial class FileChain(IServiceProvider services) : ChainService<FileContext>(services);