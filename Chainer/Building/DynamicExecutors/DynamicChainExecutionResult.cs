using System.Collections.Immutable;
using Chainer.Building.Messages;

namespace Chainer.Building.DynamicExecutors;

public sealed record DynamicChainExecutionResult<TContext>(Result<TContext> Context, ImmutableArray<ChainExecutionLog> ExecutionLogs)
    where TContext : class, ICloneable, new();