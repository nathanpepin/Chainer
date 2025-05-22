using Chainer.Core;
using Chainer.Sample.Pricing;
using Chainer.Sample.Pricing.Handlers;

namespace Chainer.Sample;

public static class ChainExecutorExample
{
    public static async Task Run()
    {
        var chains = new ChainExecutor<PriceContext>();
        chains.AddHandler(new NonCustomerFee());
        chains.AddHandler(new OldAgeDiscount());
        chains.AddHandler(new StorewideSale
        {
            Configuration = new StorewideSale.StorewideSaleConfiguration
            {
                DiscountAmount = 10
            }
        });
        chains.AddHandler(new VipDiscount());

        var customer = new Customer
        {
            Name = "Nathan Pepin",
            Age = 30,
            IsVip = true
        };
        var context = new PriceContext { Customer = customer, CurrentPrice = 100, InitialPrice = 100 };

        var result = await chains.Execute(context);
        Console.WriteLine(result);
        /*
        Context: Chainer.Sample.Pricing.PriceContext
        Success: True
        Error: None
        Start: 2025-05-21T17:25:24
        End: 2025-05-21T17:25:24
        Execution Time: 0:00:00.0156986
        Applied Handlers
                -Chainer.Sample.Pricing.Handlers.NonCustomerFee; Duration: 0:00:00.0077786
                -Chainer.Sample.Pricing.Handlers.OldAgeDiscount; Duration: 0:00:00.0005257
                -Chainer.Sample.Pricing.Handlers.StorewideSale; Duration: 0:00:00.0004569
                -Chainer.Sample.Pricing.Handlers.VipDiscount; Duration: 0:00:00.000534
        ----------------------------------------
         */
    }
}