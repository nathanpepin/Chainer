using Chainer.ChainServices.ChainBuilder.DynamicExecutors;
using Chainer.Registrations;
using Chainer.Sample.FileContextChain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register the dynamic chain from configuration
builder.Services.BindAddChain(builder.Configuration,
    "FileProcessingChain",
    "FileProcessingChain");

builder.Services.BindAddChain(builder.Configuration,
    "FileProcessingChain2",
    "FileProcessingChain2");

// Register the dynamic chain executor
builder.Services.AddScoped<IDynamicChainExecutor, DynamicChainExecutor>();

var host = builder.Build();

// Get the dynamic chain executor
var dynamicExecutor = host.Services.GetRequiredService<IDynamicChainExecutor>();

Console.WriteLine("Chain 1");
var context = new FileContext { Content = "My name,,,, is Nathan Pepin. and .I'm legit" };
var result1 = await dynamicExecutor.ExecuteChainAsync("FileProcessingChain", context);
WriteResult(result1);

Console.WriteLine("Chain 2");
var context2 = new FileContext { Content = "My name,,,, is Nathan Pepin. and .I'm legit" };
var result2 = await dynamicExecutor.ExecuteChainAsync("FileProcessingChain2", context2);
WriteResult(result2);

return;


void WriteResult(DynamicChainExecutionResult<FileContext> dynamicChainExecutionResult)
{
    if (dynamicChainExecutionResult.Context.IsSuccess)
    {
        Console.WriteLine("Chain executed successfully!");
        Console.WriteLine($"Processed Content: {dynamicChainExecutionResult.Context.Value.Content}");

        // Display the execution logs
        Console.WriteLine("\nExecution Logs:");
        foreach (var log in dynamicChainExecutionResult.ExecutionLogs)
        {
            Console.WriteLine($"- {log.HandlerTypeName.Split(',')[0].Split('.').Last()}: {log.Status}");
        }
    }
    else
    {
        Console.WriteLine($"Chain execution failed: {dynamicChainExecutionResult.Context.Error}");
    }
}