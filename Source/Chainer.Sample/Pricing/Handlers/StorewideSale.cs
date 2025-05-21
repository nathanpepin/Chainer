using Chainer.Building.Configuration;
using Chainer.Building.Configuration.Persistence;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.Pricing.Handlers;

public sealed class StorewideSale : IConfigurableChainHandler<PriceContext>, ISaveBeforeContextData, ISaveAfterContextData
{
    private StorewideSaleConfiguration? _configuration;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (_configuration is null) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice -= _configuration.DiscountAmount;

        return Task.FromResult<Result<PriceContext>>(context);
    }

    public void Configure(IHandlerConfiguration configuration)
    {
        _configuration = configuration.Bind<StorewideSaleConfiguration>();
    }

    private sealed class StorewideSaleConfiguration
    {
        public decimal DiscountAmount { get; init; }
    }
}