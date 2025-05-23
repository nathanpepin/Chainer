using Chainer.Abstractions;
using Chainer.Persistence;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.Pricing.Handlers;

public sealed class StorewideSale : IConfigurableChainHandler<PriceContext>, IContextPersistence
{
    public StorewideSaleConfiguration? Configuration { get; set; }

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (Configuration is null) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice -= Configuration.DiscountAmount;

        return Task.FromResult<Result<PriceContext>>(context);
    }

    public void Configure(IHandlerConfiguration configuration)
    {
        Configuration = configuration.Bind<StorewideSaleConfiguration>();
    }

    public PersistencePoint PersistWhen => PersistencePoint.Both;

    public sealed class StorewideSaleConfiguration
    {
        public decimal DiscountAmount { get; init; }
    }
}