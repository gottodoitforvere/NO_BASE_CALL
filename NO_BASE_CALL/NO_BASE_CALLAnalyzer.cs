using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            IMethodSymbol method = (IMethodSymbol) context.Symbol;
            if (!method.IsOverride)
            {
                return;
            }

            var basemethod = method.OverriddenMethod;
            if (basemethod == null) return;

            //Поиск всех override базового метода
            var overrides = new List<IMethodSymbol>();
            foreach (var method_i in context.Compilation.GetSymbolsWithName(basemethod.Name, SymbolFilter.Member))
            {
                IMethodSymbol candidate = (IMethodSymbol) method_i;
                if (candidate.OverriddenMethod != null && candidate.OverriddenMethod.Equals(basemethod, SymbolEqualityComparer.Default)) overrides.Add(candidate);
            }

            // Подсчет вызовов базового метода среди override + устанавливаем данный факт для текущего метода
            int base_call_count = 0;
            bool current_method_calls_base = false;
            foreach (var method_i in overrides)
            {
                var method_declaration = method_i.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
                bool this_is_current_method = SymbolEqualityComparer.Default.Equals(method, method_i);
                foreach (var node in method_declaration.Body.DescendantNodes())
                {
                    if (node is BaseExpressionSyntax)
                    {
                        base_call_count++;
                        if (this_is_current_method) current_method_calls_base = true;
                        break; 
                    }
                }
            }

            if (!current_method_calls_base && (base_call_count * 100 > Threshold * overrides.Count))
            {
                var message = string.Format(MessageFormat.ToString(), method.Name, basemethod.Name, Threshold, base_call_count, overrides.Count);
                var diagnostic = Diagnostic.Create(Rule, method.Locations[0], method.Name,basemethod.Name, Threshold, base_call_count, overrides.Count());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}