namespace Chainer.Sample.Pricing;

public sealed class PriceContext : ICloneable
{
    public decimal InitialPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public Customer? Customer { get; init; }

    public object Clone()
    {
        return new PriceContext
        {
            InitialPrice = InitialPrice,
            CurrentPrice = CurrentPrice,
            Customer = Customer
        };
    }
}