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
        public static IEnumerable<string> Mutate(string filePath, Mutatations original, Mutatations mutated)
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

        public static IEnumerable<Assembly> MutateSingleFile(string filePath, Mutatations original, Mutatations mutated)
        {
            CSharpCodeProvider csc = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            foreach (var item in Mutate(filePath, original, mutated))
            {
                yield return csc.CompileAssemblyFromSource(parameters, item).CompiledAssembly;
            }
        }

        private static class SyntaxNodeTools
        {
            public static SyntaxNode CloneNode(SyntaxNode node)
            {
                return CSharpSyntaxTree.ParseText(node.ToString()).GetRoot();
            }
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly int requestCount;
            private int count = 0;
            private bool changeMade = false;
            private readonly Mutatations Original;
            private readonly Mutatations MutateTo;
            public static IEnumerable<string> GetAllCombinations(SyntaxNode node, Mutatations original, Mutatations mutateTo)
            {
                int i = 0;
                Rewriter rewriter = new Rewriter(i, original, mutateTo);
                yield return rewriter.Visit(SyntaxNodeTools.CloneNode(node)).ToString();
                while (rewriter.changeMade)
                {
                    rewriter = new Rewriter(++i, original, mutateTo);
                    yield return rewriter.Visit(SyntaxNodeTools.CloneNode(node)).ToString();
                }
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
                    changeMade = true;
                    return base.Visit(ConvertTo(node));
                }
                return base.VisitBinaryExpression(node);
            }
        }
    }
}
