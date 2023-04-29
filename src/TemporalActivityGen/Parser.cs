using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TemporalActivityGen;

internal static class Parser
{
    public const string GenerateActivityProxyAttribute = "TemporalActivityGen.GenerateActivityProxyAttribute";

    public static bool IsInterfaceTargetForGeneration(SyntaxNode node)
        => node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0, };

    public static InterfaceDeclarationSyntax? GetInterfaceSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is not InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            return null;
        }

        foreach (AttributeListSyntax attributeListSyntax in interfaceDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == GenerateActivityProxyAttribute)
                {
                    return interfaceDeclarationSyntax;
                }
            }
        }

        return null;
    }

    public static IEnumerable<ActivityInterfaceInfo> GetTypesToGenerate(
        Compilation compilation,
        ImmutableArray<InterfaceDeclarationSyntax> targets,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken ct)
    {
        foreach (InterfaceDeclarationSyntax interfaceDeclarationSyntax in targets)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax, ct);

            if (interfaceSymbol is null)
            {
                continue;
            }

            var interfaceName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var interfaceFullyQualifiedNamespace = interfaceSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var interfaceNamespace = interfaceFullyQualifiedNamespace.Replace("global::", "");

            var proxyName = interfaceName.Split('.').Last().Replace("I", "") + "Proxy";

            var methods = interfaceSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() is { } attributeName
                        && attributeName.Contains("ActivityAttribute")));

            yield return new ActivityInterfaceInfo
            {
                interfaceName = interfaceName,
                interfaceNamespace = interfaceNamespace,
                interfaceFullyQualifiedNamespace = interfaceFullyQualifiedNamespace,
                proxyName = proxyName,
                methods = methods.Select(method => new ActivityInterfaceMethodInfo
                {
                    name = method.Name,
                    returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    isReturnGenericTask = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("System.Threading.Tasks.Task<"),
                    parameters = method.Parameters.Select(parameter => new ActivityInterfaceMethodParameterInfo
                    {
                        name = parameter.Name,
                        type = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    }).ToList(),
                    activityAttributeNameParameter = method.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() is { } attributeName
                                                   && attributeName.Contains("ActivityAttribute"))?
                        .ConstructorArguments.FirstOrDefault().Value?.ToString(),
                }).ToList(),
            };
        }

    }

    public class ActivityInterfaceInfo
    {
        public string interfaceName = string.Empty;
        public string proxyName = string.Empty;
        public string interfaceNamespace = string.Empty;
        public string interfaceFullyQualifiedNamespace = string.Empty;
        public List<ActivityInterfaceMethodInfo> methods = null!;
    }

    public class ActivityInterfaceMethodInfo
    {
        public string name = string.Empty;
        public string returnType = string.Empty;
        public bool isReturnGenericTask;
        public string? activityAttributeNameParameter;
        public List<ActivityInterfaceMethodParameterInfo> parameters = null!;
    }

    public class ActivityInterfaceMethodParameterInfo
    {
        public string name = string.Empty;
        public string type = string.Empty;
    }
}
