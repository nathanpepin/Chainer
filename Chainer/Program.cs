using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Running;
using ConsoleApp1;
using ConsoleApp1.Builder;
using CSharpFunctionalExtensions;
using Dumpify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TraceReloggerLib;
//
// public static class A
// {
//     public static async Task J()
//     {
//         
//         var builder = Host.CreateDefaultBuilder();
//
//         builder.ConfigureServices(s =>
//         {
//             var chain = new ChainExecutor<FileContext>()
//                 .AddHandler(new FileHandlerUpperCase())
//                 .AddHandler(new FileHandlerRemoveComma())
//                 .AddHandler(new FileHandlerIsLegit());
//             s.AddKeyedSingleton( "chain", chain);
//     
//             var inOutChain = new ChainInOutExecutor<FileContext, string, string[]>(
//                     x => Task.FromResult( new FileContext { Content = x.ToLowerInvariant()}), 
//                     x => Task.FromResult( x.Content.Split('.')))
//                 .AddHandler(new FileHandlerUpperCase())
//                 .AddHandler(new FileHandlerRemoveComma())
//                 .AddHandler(new FileHandlerIsLegit());
//             s.AddKeyedSingleton( "inOutChain", inOutChain);
//
//             // s.AddScoped<FileChain>();
//             s.AddScoped<FileInOutChain>();
//     
//             s.TryAddScoped<IChainHandler<FileContext>, FileHandlerUpperCase>();
//             s.AddScoped<IChainHandler<FileContext>, FileHandlerRemoveComma>();
//             s.AddScoped<IChainHandler<FileContext>, FileHandlerIsLegit>();
//         });
//
//         var h = builder.Build();
//
//         const string i = "My name,,,, is Nathan Pepin. and .I'm legit";
//         var context = new FileContext { Content = i };
//
//         var chain = h.Services.GetRequiredKeyedService<ChainExecutor<FileContext>>("chain");
//         var chainInOut = h.Services.GetRequiredKeyedService<ChainInOutExecutor<FileContext, string, string[]>>("inOutChain");
//         // var chainService = h.Services.GetRequiredService<FileChain>();
//         var inOutService = h.Services.GetRequiredService<FileInOutChain>();
//
//         var r0 = await chain.Execute(context);
//         var r1 = await chain.ExecuteWithHistory(context);
//
//         var r2 = await chainInOut.Execute(i);
//         var r3 = await chainInOut.ExecuteWithHistory(i);
//
//         // var r4 = await chainService.Execute(context);
//         // var r5 = await chainService.ExecuteWithHistory(context);
//
//         var r6 = await inOutService.Execute(i);
//         var r7 = await inOutService.ExecuteWithHistory(i);
//         ;
//     }
// }