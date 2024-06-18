namespace Chainer.ChainServices;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RegisterChains<TContext>(params Type[] types) : Attribute where TContext : class, ICloneable, new();