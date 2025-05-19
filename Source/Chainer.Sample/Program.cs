using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Chainer.Building.DynamicExecutors;
using Chainer.Building.Messages;
using Chainer.Registration;
using Chainer.Sample.FileContextChain;
using Chainer.Sample.Pricing;
using Chainer.Sample.Pricing.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

BindFromIConfiguration.AddSimpleTypeMaps<PriceContext>();
BindFromIConfiguration.AddSimpleTypeMaps<NonCustomerFee>();
BindFromIConfiguration.AddSimpleTypeMaps<OldAgeDiscount>();
BindFromIConfiguration.AddSimpleTypeMaps<StorewideSale>();
BindFromIConfiguration.AddSimpleTypeMaps<VipDiscount>();

const string fileProcessChain = "FileProcessingChain";
const string fileProcessChain2 = "FileProcessingChain2";
const string pricing = "Pricing";

// Register the dynamic chain from configuration
builder.Services.BindAddChain(builder.Configuration, fileProcessChain, fileProcessChain);
builder.Services.BindAddChain(builder.Configuration, fileProcessChain2, fileProcessChain2);
builder.Services.BindAddChain(builder.Configuration, pricing, pricing);

// Register the dynamic chain executor
builder.Services.AddScoped<IDynamicChainExecutor, DynamicChainExecutor>();

var host = builder.Build();

// Get the dynamic chain executor
var dynamicExecutor = host.Services.GetRequiredService<IDynamicChainExecutor>();

//Execute
Console.WriteLine("Chain 1");
var context = new FileContext { Content = "My name,,,, is Nathan Pepin. and .I'm legit" };
var result1 = await dynamicExecutor.ExecuteChainAsync(fileProcessChain, context);
WriteResult(result1.ExecutionLogs);

Console.WriteLine("Chain 2");
var context2 = new FileContext { Content = "My name,,,, is Nathan Pepin. and .I'm legit" };
var result2 = await dynamicExecutor.ExecuteChainAsync(fileProcessChain2, context2);
WriteResult(result2.ExecutionLogs);

Console.WriteLine("Chain 3");
var customer = new Customer
{
    Name = "Nathan Pepin",
    Age = 30,
    IsVip = true
};
var context3 = new PriceContext { Customer = customer, CurrentPrice = 100, InitialPrice = 100 };
var result3 = await dynamicExecutor.ExecuteChainAsync(pricing, context3);
WriteResult(result3.ExecutionLogs);

return;


void WriteResult(ImmutableArray<ChainExecutionLog> executionLogs)
{
    foreach (var log in executionLogs)
        Console.WriteLine($"- {log.HandlerTypeName.Split(',')[0].Split('.').Last()}: {log.Status} - {log.AfterJson}");
}