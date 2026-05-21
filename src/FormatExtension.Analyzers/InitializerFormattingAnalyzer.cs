using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FormatExtension.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InitializerFormattingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FMT0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Initializer elements should be on separate lines",
        "Initializer with multiple elements should have each element on its own line",
        "Formatting",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/clystian/DotnetFormatExtensions");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeInitializer,
            SyntaxKind.ObjectInitializerExpression,
            SyntaxKind.ArrayInitializerExpression,
            SyntaxKind.CollectionInitializerExpression);
    }

    private static void AnalyzeInitializer(SyntaxNodeAnalysisContext context)
    {
        var initializer = (InitializerExpressionSyntax)context.Node;
        var expressions = initializer.Expressions;

        if (expressions.Count <= 1)
            return;

        if (initializer.Kind() == SyntaxKind.ArrayInitializerExpression &&
            initializer.Parent is InitializerExpressionSyntax)
            return;

        if (HasMultipleExpressionsOnSameLine(expressions))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                initializer.OpenBraceToken.GetLocation()));
        }
    }

    private static bool HasMultipleExpressionsOnSameLine(SeparatedSyntaxList<ExpressionSyntax> expressions)
    {
        var lineNumber = expressions[0].GetLocation().GetLineSpan().StartLinePosition.Line;

        for (var i = 1; i < expressions.Count; i++)
        {
            var currentLine = expressions[i].GetLocation().GetLineSpan().StartLinePosition.Line;
            if (currentLine == lineNumber)
                return true;
            lineNumber = currentLine;
        }

        return false;
    }
}
