using Chainer.Execution;
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

        var result = await chains.ExecuteAsync(context);
        Console.WriteLine(result);
    }
}