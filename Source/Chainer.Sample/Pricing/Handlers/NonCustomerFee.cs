using Chainer.Building.Configuration.Persistence;
using Chainer.Core;
using Chainer.Results;
using Microsoft.Extensions.Logging;

namespace Chainer.Sample.Pricing.Handlers;

public sealed class NonCustomerFee : IChainHandler<PriceContext>, ISaveBeforeContextData, ISaveAfterContextData
{
    private const decimal NonCustomerFeeAmount = 5;

    public Task<Result<PriceContext>> Handle(PriceContext context, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (context.Customer is null) return Task.FromResult<Result<PriceContext>>(context);

        context.CurrentPrice += NonCustomerFeeAmount;

        if (context.CurrentPrice < 0) context.CurrentPrice = 0;

        return Task.FromResult<Result<PriceContext>>(context);
    }
}