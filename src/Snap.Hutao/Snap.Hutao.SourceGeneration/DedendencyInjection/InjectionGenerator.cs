﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Snap.Hutao.SourceGeneration.DedendencyInjection;

/// <summary>
/// 注入代码生成器
/// 旨在使用源生成器提高注入效率
/// 防止在运行时动态查找注入类型
/// </summary>
[Generator]
public class InjectionGenerator : ISourceGenerator
{
    private const string InjectAsSingletonName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Singleton";
    private const string InjectAsTransientName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Transient";

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new InjectionSyntaxContextReceiver());
    }

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        // retrieve the populated receiver
        if (context.SyntaxContextReceiver is not InjectionSyntaxContextReceiver receiver)
        {
            return;
        }

        string toolName = this.GetGeneratorType().FullName;

        StringBuilder sourceCodeBuilder = new();
        sourceCodeBuilder.Append($@"// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// This class is generated by Snap.Hutao.SourceGeneration

using Microsoft.Extensions.DependencyInjection;

namespace Snap.Hutao.Core.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{toolName}"",""1.0.0.0"")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public static partial IServiceCollection AddInjections(this IServiceCollection services)
    {{");

        FillWithInjectionServices(receiver, sourceCodeBuilder);
        sourceCodeBuilder.Append(@"
        return services;
    }
}");

        context.AddSource("ServiceCollectionExtensions.g.cs", SourceText.From(sourceCodeBuilder.ToString(), Encoding.UTF8));
    }

    private static void FillWithInjectionServices(InjectionSyntaxContextReceiver receiver, StringBuilder sourceCodeBuilder)
    {
        List<string> lines = new();
        StringBuilder lineBuilder = new();

        foreach (INamedTypeSymbol classSymbol in receiver.Classes)
        {
            lineBuilder
                .Clear()
                .Append("\r\n");

            AttributeData injectionInfo = classSymbol
                .GetAttributes()
                .Single(attr => attr.AttributeClass!.ToDisplayString() == InjectionSyntaxContextReceiver.AttributeName);
            ImmutableArray<TypedConstant> arguments = injectionInfo.ConstructorArguments;

            TypedConstant injectAs = arguments[0];

            string injectAsName = injectAs.ToCSharpString();
            switch (injectAsName)
            {
                case InjectAsSingletonName:
                    lineBuilder.Append(@"        services.AddSingleton(");
                    break;
                case InjectAsTransientName:
                    lineBuilder.Append(@"        services.AddTransient(");
                    break;
                default:
                    throw new InvalidOperationException($"非法的InjectAs值: [{injectAsName}]");
            }

            if (arguments.Length == 2)
            {
                TypedConstant interfaceType = arguments[1];
                lineBuilder.Append($"{interfaceType.ToCSharpString()}, ");
            }

            lineBuilder.Append($"typeof({classSymbol.ToDisplayString()}));");

            lines.Add(lineBuilder.ToString());
        }

        foreach (string line in lines.OrderBy(x => x))
        {
            sourceCodeBuilder.Append(line);
        }
    }
}
