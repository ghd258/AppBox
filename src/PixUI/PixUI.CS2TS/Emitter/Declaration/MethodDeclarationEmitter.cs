using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PixUI.CS2TS
{
    partial class Emitter
    {
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //暂时忽略一些特殊方法 eg: GetHashCode
            if (TryIgnoreMethod(node)) return;

            var isAbstract = node.HasAbstractModifier();
            if (isAbstract && ToJavaScript) return;

            //try TSRawScriptAttribute
            if (node.IsTSRawScript(out var script))
            {
                Write(script!);
                return;
            }

            WriteLeadingTrivia(node);
            if (node.Parent is not InterfaceDeclarationSyntax)
                WriteModifiers(node.Modifiers);

            // method name, maybe renamed by TSRenameAttribute
            var methodName = node.Identifier.Text;
            TryRenameMethod(node, ref methodName);
            Write(methodName);

            // generic type parameters
            if (!ToJavaScript)
                WriteTypeParameters(node.TypeParameterList, node.ConstraintClauses);

            // parameters
            Write('(');
            VisitSeparatedList(node.ParameterList.Parameters);
            Write(')');

            // return type
            var isReturnVoid = node.IsVoidReturnType();
            if (!ToJavaScript)
            {
                if (!isReturnVoid)
                {
                    Write(": ");
                    DisableVisitLeadingTrivia();
                    Visit(node.ReturnType);
                    EnableVisitLeadingTrivia();
                }
                else if (node.Parent is InterfaceDeclarationSyntax || node.HasAbstractModifier())
                {
                    Write(": void");
                }

                // TrailingTrivia when has ConstraintClauses
                if (node.ConstraintClauses.Any())
                    WriteTrailingTrivia(node.ConstraintClauses.Last());
                else
                    VisitTrailingTrivia(node.ParameterList.CloseParenToken);
            }

            // abstract
            if (isAbstract)
            {
                VisitToken(node.SemicolonToken);
                return;
            }

            // body
            if (node.Body != null)
            {
                Visit(node.Body);
                return;
            }

            //TypeScript不支持ExpressionBody
            if (node.ExpressionBody != null)
            {
                Write(" { ");
                if (!isReturnVoid && node.ExpressionBody.Expression is not ThrowExpressionSyntax)
                    Write("return ");
                Visit(node.ExpressionBody!.Expression);
                Write("; }");
                VisitTrailingTrivia(node.SemicolonToken);

                return;
            }

            // interface method
            VisitToken(node.SemicolonToken);
        }
        
        /// <summary>
        /// 暂忽略一些特殊方法的转换
        /// </summary>
        private static bool TryIgnoreMethod(MethodDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.Kind() == SyntaxKind.OverrideKeyword))
            {
                //TODO:精确判断，暂简单实现
                if (node.Identifier.Text == "GetHashCode" &&
                    node.ParameterList.Parameters.Count == 0)
                    return true;

                if (node.Identifier.Text == "Equals" && node.ParameterList.Parameters.Count == 1)
                {
                    var para = node.ParameterList.Parameters[0];
                    var typeString = para.Type!.ToString();
                    return typeString.StartsWith("object");
                }
            }

            return false;
        }

        /// <summary>
        /// 用于重命名
        /// 1.标记为TSRenameAttribute的方法定义,以解决无法重载方法的问题
        /// 2.override ToString()
        /// </summary>
        private static void TryRenameMethod(MethodDeclarationSyntax node, ref string name)
        {
            if (node.Identifier.Text == "ToString" && node.ParameterList.Parameters.Count == 0)
            {
                name = "toString";
                return;
            }

            var attribute = SyntaxExtensions.TryGetAttribute(node.AttributeLists, Emitter.IsTSRenameAttribute);
            if (attribute == null) return;

            var nameLiteral = (LiteralExpressionSyntax)attribute.ArgumentList!.Arguments[0].Expression;
            name = nameLiteral.Token.ValueText;
        }
    }
    
}