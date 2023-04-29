using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TemporalActivityGen;

/// <inheritdoc />
[Generator]
public class ActivityProxyGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i =>
        {
            i.AddSource("GenerateActivityProxyAttribute.g.cs", EmbeddedSources.GenerateActivityProxyAttributeSource);
        });

        IncrementalValuesProvider<InterfaceDeclarationSyntax> interfaceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => Parser.IsInterfaceTargetForGeneration(s),
                transform: static (ctx, _) => Parser.GetInterfaceSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<ImmutableArray<InterfaceDeclarationSyntax>> targets
            = interfaceDeclarations.Collect();

        IncrementalValueProvider<(Compilation Left, ImmutableArray<InterfaceDeclarationSyntax> Right)> compilationAndValues
            = context.CompilationProvider.Combine(targets);

        context.RegisterSourceOutput(compilationAndValues,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static void Execute(
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> interfaces,
        SourceProductionContext context)
    {
        if (interfaces.IsDefaultOrEmpty)
        {
            return;
        }

        var proxiesToGenerate = Parser.GetTypesToGenerate(compilation, interfaces, context.ReportDiagnostic, context.CancellationToken);
        if (proxiesToGenerate == null)
        {
            return;
        }

        var sb = new StringBuilder();
        var resources = EmbeddedSources.DefaultResources;

        foreach (var proxyToGenerate in proxiesToGenerate)
        {
            sb.Clear();

            sb.AppendLine(resources.Header);
            if (resources.NullableEnable)
            {
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
            }

            sb.AppendLine(resources.DefaultUsings);

            sb.AppendLine(EmbeddedSources.CreateActivityProxyRegistration($"typeof({proxyToGenerate.interfaceFullyQualifiedNamespace}.{proxyToGenerate.proxyName})"));
            sb.AppendLine();

            sb.AppendLine($"namespace {proxyToGenerate.interfaceNamespace};");
            sb.AppendLine();

            sb.AppendLine($"public partial class {proxyToGenerate.proxyName} : {proxyToGenerate.interfaceName}");
            sb.AppendLine("{");

            sb.AppendLine("    private readonly global::System.IServiceProvider _serviceProvider;");
            sb.AppendLine();
            sb.AppendLine($"    public {proxyToGenerate.proxyName}(global::System.IServiceProvider serviceProvider)");
            sb.AppendLine("    {");
            sb.AppendLine("        _serviceProvider = serviceProvider;");
            sb.AppendLine("    }");
            sb.AppendLine();

            for (var i = 0; i < proxyToGenerate.methods.Count; i++)
            {
                Parser.ActivityInterfaceMethodInfo? method = proxyToGenerate.methods[i];
                if (method.activityAttributeNameParameter != null)
                {
                    sb.AppendLine($"    [Activity(\"{method.activityAttributeNameParameter}\")]");
                }
                else
                {
                    sb.AppendLine($"    [Activity]");
                }
                sb.AppendLine($"    public async {method.returnType} {method.name}({string.Join(", ", method.parameters.Select(p => $"{p.type} {p.name}"))})");
                sb.AppendLine("    {");
                sb.AppendLine("        await using var scope = _serviceProvider.CreateAsyncScope();");
                sb.AppendLine($"        var impl = scope.ServiceProvider.GetRequiredService<{proxyToGenerate.interfaceName}>();");
                if (method.isReturnGenericTask)
                {
                    sb.AppendLine($"        return await impl.{method.name}({string.Join(", ", method.parameters.Select(p => p.name))});");
                }
                else
                {
                    sb.AppendLine($"        await impl.{method.name}({string.Join(", ", method.parameters.Select(p => p.name))});");
                }
                sb.AppendLine("    }");

                if (i < proxyToGenerate.methods.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");

            context.AddSource($"{proxyToGenerate.proxyName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
