using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FormatExtension.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitializerFormattingCodeFix))]
[Shared]
public class InitializerFormattingCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(InitializerFormattingAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        var initializer = root.FindNode(diagnostic.Location.SourceSpan)
            .AncestorsAndSelf()
            .OfType<InitializerExpressionSyntax>()
            .FirstOrDefault();

        if (initializer is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Format initializer elements on separate lines",
                createChangedDocument: c => FormatInitializerAsync(context.Document, initializer, c),
                equivalenceKey: nameof(InitializerFormattingCodeFix)),
            diagnostic);
    }

    private static async Task<Document> FormatInitializerAsync(
        Document document,
        InitializerExpressionSyntax initializer,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var (indentSize, useTabs) = DetectIndentation(root);
        var parentLineSpan = initializer.Parent!.GetLocation().GetLineSpan();
        var baseIndent = GetLineLeadingWhitespace(root, parentLineSpan.StartLinePosition.Line);

        var prevToken = initializer.OpenBraceToken.GetPreviousToken();
        var newPrevToken = prevToken.WithTrailingTrivia();

        var reformatted = ReformatInitializer(initializer, baseIndent, indentSize, useTabs);

        var newRoot = root.ReplaceNode(initializer, reformatted);
        var prevInNewRoot = newRoot.FindToken(prevToken.SpanStart, findInsideTrivia: false);
        if (prevInNewRoot.Span == prevToken.Span)
        {
            newRoot = newRoot.ReplaceToken(prevInNewRoot, newPrevToken);
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static InitializerExpressionSyntax ReformatInitializer(
        InitializerExpressionSyntax initializer,
        string baseIndent,
        int indentSize,
        bool useTabs)
    {
        var elementIndent = useTabs
            ? baseIndent + "\t"
            : baseIndent + new string(' ', indentSize);

        var exprList = new List<ExpressionSyntax>();
        var sepList = new List<SyntaxToken>();

        for (var i = 0; i < initializer.Expressions.Count; i++)
        {
            var node = initializer.Expressions[i];
            var formatted = ReformatExpression(node, elementIndent, indentSize, useTabs);

            // Only first expression gets leading indent; separator provides it for the rest
            var cleaned = i == 0
                ? formatted.WithLeadingTrivia(SyntaxFactory.Whitespace(elementIndent))
                : formatted.WithLeadingTrivia();

            exprList.Add(cleaned);

            if (i < initializer.Expressions.Count - 1)
            {
                var separator = SyntaxFactory.Token(SyntaxKind.CommaToken)
                    .WithTrailingTrivia(
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.Whitespace(elementIndent));
                sepList.Add(separator);
            }
        }

        var openBrace = initializer.OpenBraceToken;

        var newOpenBrace = openBrace
            .WithLeadingTrivia(SyntaxFactory.LineFeed, SyntaxFactory.Whitespace(baseIndent))
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        var newInitializer = SyntaxFactory.InitializerExpression(
            initializer.Kind(),
            SyntaxFactory.SeparatedList(exprList, sepList))
            .WithOpenBraceToken(newOpenBrace)
            .WithCloseBraceToken(
                initializer.CloseBraceToken.WithLeadingTrivia(
                    SyntaxFactory.LineFeed,
                    SyntaxFactory.Whitespace(baseIndent)));

        return newInitializer;
    }

    private static ExpressionSyntax ReformatExpression(
        ExpressionSyntax node,
        string elementIndent,
        int indentSize,
        bool useTabs)
    {
        var innerInitializers = node.DescendantNodesAndSelf()
            .OfType<InitializerExpressionSyntax>()
            .Where(i => i.Kind() != SyntaxKind.ArrayInitializerExpression || i.Parent is not InitializerExpressionSyntax)
            .ToList();

        if (innerInitializers.Count == 0)
        {
            return node.WithoutTrivia();
        }

        var rewritten = node.ReplaceNodes(
            innerInitializers,
            (original, _) =>
            {
                var innerElementIndent = useTabs
                    ? elementIndent + "\t"
                    : elementIndent + new string(' ', indentSize);

                return BuildFormattedInitializer(original, elementIndent, innerElementIndent, indentSize, useTabs);
            });

        return rewritten.WithoutTrivia();
    }

    private static InitializerExpressionSyntax BuildFormattedInitializer(
        InitializerExpressionSyntax initializer,
        string baseIndent,
        string elementIndent,
        int indentSize,
        bool useTabs)
    {
        var exprList = new List<ExpressionSyntax>();
        var sepList = new List<SyntaxToken>();

        for (var i = 0; i < initializer.Expressions.Count; i++)
        {
            var node = initializer.Expressions[i];

            var childInitializers = node.DescendantNodesAndSelf()
                .OfType<InitializerExpressionSyntax>()
                .Where(i => i.Kind() != SyntaxKind.ArrayInitializerExpression || i.Parent is not InitializerExpressionSyntax)
                .ToList();

            ExpressionSyntax cleaned;
            if (childInitializers.Count > 0)
            {
                cleaned = node.ReplaceNodes(
                    childInitializers,
                    (original, _) =>
                    {
                        var childIndent = useTabs
                            ? elementIndent + "\t"
                            : elementIndent + new string(' ', indentSize);
                        return BuildFormattedInitializer(
                            original, elementIndent, childIndent, indentSize, useTabs);
                    });
                cleaned = cleaned.WithoutTrivia();
            }
            else
            {
                cleaned = node.WithoutTrivia();
            }

            // Only first expression gets leading indent; separator provides it for the rest
            if (i == 0)
            {
                cleaned = cleaned.WithLeadingTrivia(SyntaxFactory.Whitespace(elementIndent));
            }
            else
            {
                cleaned = cleaned.WithLeadingTrivia();
            }

            exprList.Add(cleaned);

            if (i < initializer.Expressions.Count - 1)
            {
                var separator = SyntaxFactory.Token(SyntaxKind.CommaToken)
                    .WithTrailingTrivia(
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.Whitespace(elementIndent));
                sepList.Add(separator);
            }
        }

        var openBrace = initializer.OpenBraceToken;

        var newOpenBrace = openBrace
            .WithLeadingTrivia(SyntaxFactory.LineFeed, SyntaxFactory.Whitespace(baseIndent))
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        return SyntaxFactory.InitializerExpression(
            initializer.Kind(),
            SyntaxFactory.SeparatedList(exprList, sepList))
            .WithOpenBraceToken(newOpenBrace)
            .WithCloseBraceToken(
                initializer.CloseBraceToken.WithLeadingTrivia(
                    SyntaxFactory.LineFeed,
                    SyntaxFactory.Whitespace(baseIndent)));
    }

    private static string GetLineLeadingWhitespace(SyntaxNode root, int lineNumber)
    {
        var text = root.SyntaxTree.GetText();
        if (lineNumber >= text.Lines.Count)
            return "";

        var lineText = text.Lines[lineNumber].ToString();
        return new string(lineText.TakeWhile(char.IsWhiteSpace).ToArray());
    }

    private static (int indentSize, bool useTabs) DetectIndentation(SyntaxNode root)
    {
        var text = root.SyntaxTree.GetText();
        foreach (var line in text.Lines)
        {
            var lineText = line.ToString();
            var trimmed = lineText.TrimStart();
            if (trimmed.Length == 0 || trimmed.Length == lineText.Length)
                continue;

            var leading = lineText.Substring(0, lineText.Length - trimmed.Length);

            if (leading.Contains('\t'))
                return (4, true);

            if (leading.Length >= 2)
            {
                var size = leading.Length;
                if (size % 2 == 0)
                    return (size, false);
                return (size, false);
            }
        }

        return (4, false);
    }
}
