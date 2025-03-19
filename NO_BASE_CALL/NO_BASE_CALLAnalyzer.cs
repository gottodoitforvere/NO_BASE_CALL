using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NO_BASE_CALL
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NO_BASE_CALLAnalyzer : DiagnosticAnalyzer
    {
        const int Threshold = 65;
        public const string DiagnosticId = "NO_BASE_CALL_STAT";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        private struct Statistic
        {
            public int override_count;
            public int override_with_basecall;
            public List<IMethodSymbol> methods_without_base_call;
        }
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilation_start =>
            {
                var dictionary = new ConcurrentDictionary<IMethodSymbol, Statistic>();
                compilation_start.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var method_decl = (MethodDeclarationSyntax)syntaxContext.Node;
                    var method_symb = syntaxContext.SemanticModel.GetDeclaredSymbol(method_decl, syntaxContext.CancellationToken);
                    if (!method_symb.IsOverride)
                        return;

                    var basemethod = method_symb.OverriddenMethod;
                    if (basemethod == null) return;

                    bool current_method_calls_base = false;
                    foreach (var invocation in method_decl.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            memberAccess.Expression is BaseExpressionSyntax &&
                            syntaxContext.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol is IMethodSymbol calledmethod &&
                            calledmethod.Equals(basemethod, SymbolEqualityComparer.Default))
                        {
                            current_method_calls_base = true;
                        }
                    }

                    dictionary.AddOrUpdate(
                        key: basemethod,
                        addValueFactory: lambda => new Statistic
                        {
                            override_count = 1,
                            override_with_basecall = current_method_calls_base ? 1 : 0,
                            methods_without_base_call = current_method_calls_base ? new List<IMethodSymbol>(): new List<IMethodSymbol> {method_symb}
                        },
                        updateValueFactory: (lambda, existing) =>
                        {
                            if (!current_method_calls_base)
                            {
                                existing.methods_without_base_call.Add(method_symb);
                            }
                            return new Statistic
                            {
                                override_count = existing.override_count + 1,
                                override_with_basecall = existing.override_with_basecall + (current_method_calls_base ? 1 : 0),
                                methods_without_base_call = existing.methods_without_base_call
                            };
                        });
                }, SyntaxKind.MethodDeclaration);

                compilation_start.RegisterCompilationEndAction(compilation_end =>
                {
                    foreach (var elem in dictionary)
                    {
                        var basemethod = elem.Key;
                        var stat = elem.Value;
                        if (stat.override_with_basecall * 100 > Threshold * stat.override_count)
                        {
                            foreach (var method in stat.methods_without_base_call)
                            {
                                var diagnostic = Diagnostic.Create(Rule, method.Locations[0], method.Name, basemethod.Name, Threshold, stat.override_with_basecall, stat.override_count);
                                compilation_end.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                });
            });
        }
    }
}
