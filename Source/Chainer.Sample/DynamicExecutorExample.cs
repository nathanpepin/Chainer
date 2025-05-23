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
        var dynamicExecutor = host.Services.GetRequiredService<IDynamicChainExecutor>();

        var customer = new Customer
        {
            Name = "Nathan Pepin",
            Age = 30,
            IsVip = true
        };
        var context = new PriceContext { Customer = customer, CurrentPrice = 100, InitialPrice = 100 };
        var result = await dynamicExecutor.ExecuteChainAsync(Pricing, context);
        Console.WriteLine(result);

        /*
        - VipDiscount: Completed - {"InitialPrice":100,"CurrentPrice":35,"Customer":{"Name":"Nathan Pepin","Age":30,"IsVip":true}}
        - OldAgeDiscount: Completed - {"InitialPrice":100,"CurrentPrice":35,"Customer":{"Name":"Nathan Pepin","Age":30,"IsVip":true}}
        - StorewideSale: Completed - {"InitialPrice":100,"CurrentPrice":34.95,"Customer":{"Name":"Nathan Pepin","Age":30,"IsVip":true}}
        - NonCustomerFee: Completed - {"InitialPrice":100,"CurrentPrice":39.95,"Customer":{"Name":"Nathan Pepin","Age":30,"IsVip":true}}
         */
    }
}