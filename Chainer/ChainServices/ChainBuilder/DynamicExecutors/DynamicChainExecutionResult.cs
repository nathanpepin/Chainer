using System.Collections.Immutable;
using Chainer.ChainServices.ChainBuilder.Messages;

namespace Chainer.ChainServices.ChainBuilder.DynamicExecutors;

public sealed record DynamicChainExecutionResult<TContext>(Result<TContext> Context, ImmutableArray<ChainExecutionLog> ExecutionLogs)
    where TContext : class, ICloneable, new();