using Chainer.Sample;

var divider = new string('~', 20);

Console.WriteLine("Chain Executor");
Console.WriteLine(divider);
await ChainExecutorExample.Run();

Console.WriteLine("Dependency Injected Chain Executor");
Console.WriteLine(divider);
await DependencyInjectedChainExecutorExample.Run(args);

Console.WriteLine("Dynamic  Executor");
Console.WriteLine(divider);
await DynamicExecutorExample.Run(args);