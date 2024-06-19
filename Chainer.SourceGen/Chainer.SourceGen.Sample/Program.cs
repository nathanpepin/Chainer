using System;
using Chainer.ChainServices;
using Chainer.SourceGen.Sample.FileContextChain;
using Chainer.SourceGen.Sample.FileContextChain.Chains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services.RegisterChains();

var host = builder.Build();

const string i = "My name,,,, is Nathan Pepin. and .I'm legit";
var context = new FileContext { Content = i };

var chain = host.Services.GetRequiredService<FileChain>();

var executeOutput = chain.Execute(context).Result;
Console.WriteLine(executeOutput);

var executeWithHistoryOutput = chain.ExecuteWithHistory(context).Result;
Console.WriteLine(executeWithHistoryOutput);

var executeWithHistoryWithoutCloneOutput = chain.ExecuteWithHistory(context, false).Result;
Console.WriteLine(executeWithHistoryWithoutCloneOutput);