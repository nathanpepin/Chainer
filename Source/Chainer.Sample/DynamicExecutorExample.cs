using Chainer.Abstractions;
using Chainer.Execution;
using Chainer.Registration;
using Chainer.Sample.Pricing;
using Chainer.Sample.Pricing.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chainer.Sample;

public static class DynamicExecutorExample
{
    private const string Pricing = "Pricing";

    public static async Task Run(string[] strings)
    {
        var builder = Host.CreateApplicationBuilder(strings);

        ConfigurationRegistrationExtensions.AddSimpleTypeMaps<PriceContext>();
        ConfigurationRegistrationExtensions.AddSimpleTypeMaps<NonCustomerFee>();
        ConfigurationRegistrationExtensions.AddSimpleTypeMaps<OldAgeDiscount>();
        ConfigurationRegistrationExtensions.AddSimpleTypeMaps<StorewideSale>();
        ConfigurationRegistrationExtensions.AddSimpleTypeMaps<VipDiscount>();


        // Register the dynamic chain from configuration
        builder.Services.AddChainFromConfiguration(builder.Configuration, Pricing);

        // Register the dynamic chain executor
        builder.Services.AddScoped<IDynamicChainExecutor, DynamicChainExecutor>();

        var host = builder.Build();

        // Get the dynamic chain executor
        var executor = host.Services.GetRequiredService<IDynamicChainExecutor>();

        var customer = new Customer
        {
            Name = "Nathan Pepin",
            Age = 30,
            IsVip = true
        };
        var context = new PriceContext { Customer = customer, CurrentPrice = 100, InitialPrice = 100 };
        var result = await executor.ExecuteChainAsync(Pricing, context);
        Console.WriteLine(result);
    }
}