using Chainer.Building.Configuration;
using Chainer.Core;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.Pricing.Handlers;

public sealed class VipDiscount : IChainHandler<PriceContext>, ISaveBeforeContextData, ISaveAfterContextData
{
    private const decimal VipDiscountAmount = 65;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Customer?.IsVip is not true) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice -= VipDiscountAmount;

        if (context.CurrentPrice < 0) context.CurrentPrice = 0;

        return Task.FromResult<Result<PriceContext>>(context);
    }
}