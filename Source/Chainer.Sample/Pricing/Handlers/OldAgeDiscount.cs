using Chainer.Building.Configuration;
using Chainer.Core;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.Pricing.Handlers;

public sealed class OldAgeDiscount : IChainHandler<PriceContext>, ISaveBeforeContextData, ISaveAfterContextData
{
    private const int MinimumAge = 65;
    private const decimal OldAgeDiscountAmount = 0.9m;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Customer?.Age is null or < MinimumAge) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice *= OldAgeDiscountAmount;

        return Task.FromResult<Result<PriceContext>>(context);
    }
}