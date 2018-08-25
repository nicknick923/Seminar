using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Mutator
{
    public static class Mutator
    {
        public static IEnumerable<MutationResult> Mutate(string filePath, Mutatations original, Mutatations mutated)
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath)).GetRoot();
            return Rewriter.GetAllCombinations(root, original, mutated);
        }

        public enum Mutatations
        {
            Add,
            Subract,
            Multiply,
            Divide
        }

        private static class SyntaxNodeTools
        {
            public static SyntaxNode CloneNode(SyntaxNode node)
            {
                return CSharpSyntaxTree.ParseText(node.ToString()).GetRoot();
            }
        }

        public class MutationResult
        {
            public string Summary { get; }
            public string Result { get; }
            public Assembly Assembly { get; }
            public MutationResult(string summary, string result)
            {
                Summary = summary;
                Result = result;
                CSharpCodeProvider csc = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                Assembly = csc.CompileAssemblyFromSource(parameters, result).CompiledAssembly;
            }
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly int requestCount;
            private int count = 0;
            private bool changeMade = false;
            private readonly Mutatations Original;
            private readonly Mutatations MutateTo;
            private string ChangeNote;
            public static IEnumerable<MutationResult> GetAllCombinations(SyntaxNode node, Mutatations original, Mutatations mutateTo)
            {
                List<MutationResult> results = new List<MutationResult>();
                int i = 0;
                Rewriter rewriter = new Rewriter(i, original, mutateTo);
                SyntaxNode visitedNode = rewriter.Visit(SyntaxNodeTools.CloneNode(node));

                results.Add(new MutationResult(rewriter.ChangeNote, visitedNode.ToString()));
                while (rewriter.changeMade)
                {
                    rewriter = new Rewriter(++i, original, mutateTo);
                    visitedNode = rewriter.Visit(SyntaxNodeTools.CloneNode(node));
                    results.Add(new MutationResult(rewriter.ChangeNote, visitedNode.ToString()));
                }
                results.RemoveAt(results.Count - 1);
                return results;
            }
            private Rewriter(int inRequestCount, Mutatations original, Mutatations mutateTo)
            {
                Original = original;
                MutateTo = mutateTo;
                requestCount = inRequestCount;
            }

            private ExpressionSyntax ConvertTo(BinaryExpressionSyntax original)
            {
                char operatorChar = ' ';
                switch (MutateTo)
                {
                    case Mutatations.Add:
                        operatorChar = '+';
                        break;
                    case Mutatations.Subract:
                        operatorChar = '-';
                        break;
                    case Mutatations.Multiply:
                        operatorChar = '*';
                        break;
                    case Mutatations.Divide:
                        operatorChar = '/';
                        break;
                    default:
                        throw new Exception();
                }
                return SyntaxFactory.ParseExpression($"{original.Left} {operatorChar} {original.Right}");
            }

            public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if ((Original == Mutatations.Add && node.IsKind(SyntaxKind.AddExpression)
                    || Original == Mutatations.Subract && node.IsKind(SyntaxKind.SubtractExpression)
                    || Original == Mutatations.Multiply && node.IsKind(SyntaxKind.MultiplyExpression)
                    || Original == Mutatations.Divide && node.IsKind(SyntaxKind.DivideExpression))
                    && count++ == requestCount)
                {
                    ExpressionSyntax expressionSyntax = ConvertTo(node);
                    ChangeNote = $"[{node.ToString()}] Changed To [{expressionSyntax.ToString()}]";
                    changeMade = true;
                    return base.Visit(expressionSyntax);
                }
                return base.VisitBinaryExpression(node);
            }
        }
    }
}
