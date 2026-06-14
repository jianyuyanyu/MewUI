using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aprillz.MewUI.Analyzers;

// Shared chain layout used by MEW1102 (expand / collapse refactoring) and MEW1101 (which expands the
// chain it produces). Rebuilds the chain from its structure, ignoring existing trivia, so it is
// idempotent; recurses into arguments that are themselves chains to produce a markup tree.
internal static class FluentChainLayout
{
    public const string Unit = "    ";
    public const int MinLinks = 2;

    // Number of chained `.Method(...)` calls in the expression (0 if it is not a member-access chain).
    public static int ChainLength(ExpressionSyntax expression)
    {
        var count = 0;
        var current = expression;
        while (current is InvocationExpressionSyntax invocation
               && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            count++;
            current = access.Expression;
        }

        return count;
    }

    public static ExpressionSyntax Format(InvocationExpressionSyntax top, string baseIndent, bool expand, string newline, SemanticModel model)
    {
        var calls = new List<(SimpleNameSyntax Name, ArgumentListSyntax Arguments)>();
        ExpressionSyntax current = top;
        while (current is InvocationExpressionSyntax invocation
               && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            calls.Add((access.Name, invocation.ArgumentList));
            current = access.Expression;
        }

        calls.Reverse();

        var dotIndent = baseIndent + Unit;
        var dotLeading = expand ? Break(dotIndent, newline) : default;

        ExpressionSyntax built = current.WithoutTrivia();
        foreach (var (name, arguments) in calls)
        {
            var memberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                built,
                SyntaxFactory.Token(SyntaxKind.DotToken).WithLeadingTrivia(dotLeading),
                name.WithoutTrivia());
            built = SyntaxFactory.InvocationExpression(memberAccess, FormatArguments(arguments, dotIndent, expand, newline, model));
        }

        return built;
    }

    // An element chain (rooted in `new X()`, a call, or a value) is expanded as its own tree; a chain
    // rooted in a type (e.g. `Color.FromRgb(...)`, a static factory) is a value and stays inline.
    private static bool IsElementChain(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax || ChainLength(expression) < 1)
        {
            return false;
        }

        ExpressionSyntax root = expression;
        while (root is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            root = access.Expression;
        }

        if (root is ObjectCreationExpressionSyntax or InvocationExpressionSyntax)
        {
            return true;
        }

        var symbol = model.GetSymbolInfo(root).Symbol;
        return symbol is not null and not ITypeSymbol and not INamespaceSymbol;
    }

    // Break the argument list onto its own lines when it has element children, expanding each as a
    // tree (separated by a blank line). Value arguments and type-rooted calls stay inline.
    private static ArgumentListSyntax FormatArguments(ArgumentListSyntax arguments, string callIndent, bool expand, string newline, SemanticModel model)
    {
        var list = arguments.Arguments;
        var breakList = expand && list.Any(argument => IsElementChain(argument.Expression, model));

        if (!breakList)
        {
            var inline = list.Select(argument => SyntaxFactory.Argument(
                argument.NameColon, argument.RefKindKeyword, InlineValue(argument.Expression, newline, model)));
            var spacedCommas = Enumerable.Repeat(
                SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space),
                System.Math.Max(0, list.Count - 1));
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(inline, spacedCommas));
        }

        var argIndent = callIndent + Unit;
        var firstLeading = Break(argIndent, newline);
        // Siblings are separated by a blank line.
        var siblingLeading = SyntaxFactory.TriviaList(
            SyntaxFactory.EndOfLine(newline), SyntaxFactory.EndOfLine(newline), SyntaxFactory.Whitespace(argIndent));

        var formatted = list.Select((argument, index) =>
        {
            var value = IsElementChain(argument.Expression, model)
                ? Format((InvocationExpressionSyntax)argument.Expression, argIndent, expand: true, newline, model)
                : InlineValue(argument.Expression, newline, model);
            return SyntaxFactory.Argument(argument.NameColon, argument.RefKindKeyword, value)
                .WithLeadingTrivia(index == 0 ? firstLeading : siblingLeading);
        });

        var commas = Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), System.Math.Max(0, list.Count - 1));
        return SyntaxFactory.ArgumentList(
            SyntaxFactory.Token(SyntaxKind.OpenParenToken),
            SyntaxFactory.SeparatedList(formatted, commas),
            SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithLeadingTrivia(Break(callIndent, newline)));
    }

    // An inline argument: collapse a nested chain back to one line, otherwise just strip outer trivia.
    private static ExpressionSyntax InlineValue(ExpressionSyntax expression, string newline, SemanticModel model)
        => expression is InvocationExpressionSyntax chain && ChainLength(expression) >= 1
            ? Format(chain, string.Empty, expand: false, newline, model)
            : expression.WithoutTrivia();

    private static SyntaxTriviaList Break(string indent, string newline)
        => SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline), SyntaxFactory.Whitespace(indent));
}
