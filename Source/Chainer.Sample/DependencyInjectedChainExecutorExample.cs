using Chainer.Abstractions;
using Chainer.Execution;
using Chainer.Registration;
using Chainer.Sample.Pricing;
using Chainer.Sample.Pricing.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample;

public static class DependencyInjectedChainExecutorExample
{
    private const string Pricing = "Pricing";

    public sealed class PricingExecutor(IServiceProvider services, ILogger<DependencyInjectedChainExecutor<PriceContext>> logger)
        : DependencyInjectedChainExecutor<PriceContext>(services, logger)
    {
        protected override List<Type> ChainHandlers { get; } =
        [
            typeof(NonCustomerFee),
            typeof(OldAgeDiscount),
            typeof(StorewideSale),
            typeof(VipDiscount)
        ];
    }

    public static async Task Run(string[] strings)
    {
        var builder = Host.CreateApplicationBuilder(strings);

        // Register services
        builder.Services.AddSingleton<PriceContext>();
        builder.Services.AddSingleton<NonCustomerFee>();
        builder.Services.AddSingleton<OldAgeDiscount>();
        builder.Services.AddSingleton<StorewideSale>(_ => new StorewideSale
        {
            Configuration = new StorewideSale.StorewideSaleConfiguration
            {
                DiscountAmount = 10
            }
        });
        builder.Services.AddSingleton<VipDiscount>();

        // Register the dynamic chain executor
        builder.Services.AddSingleton<PricingExecutor>();

        var host = builder.Build();

        // Get the dynamic chain executor
        var executor = host.Services.GetRequiredService<PricingExecutor>();

        var customer = new Customer
        {
            Name = "Nathan Pepin",
            Age = 30,
            IsVip = true
        };
        var context = new PriceContext { Customer = customer, CurrentPrice = 100, InitialPrice = 100 };
        var result = await executor.ExecuteAsync(context);
        Console.WriteLine(result);
    }
}