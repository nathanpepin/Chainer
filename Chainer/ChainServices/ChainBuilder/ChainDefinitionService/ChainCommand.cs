using Chainer.ChainServices.ChainBuilder.ConfigurationHandlers;

namespace Chainer.ChainServices.ChainBuilder.ChainDefinitionService;

public sealed record ChainCommand(Type HandlerType, object Configuration, HandlerConfigurationType ConfigurationType, int Order);