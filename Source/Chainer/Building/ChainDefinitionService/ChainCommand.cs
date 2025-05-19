using Chainer.Building.Configuration;

namespace Chainer.Building.ChainDefinitionService;

public sealed record ChainCommand(Type HandlerType, object Configuration, HandlerConfigurationType ConfigurationType, int Order);