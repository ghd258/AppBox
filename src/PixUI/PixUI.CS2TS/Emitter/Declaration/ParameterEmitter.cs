using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PixUI.CS2TS
{
    partial class Emitter
    {
        public override void VisitParameter(ParameterSyntax node)
        {
            Write(node.Identifier.Text);

            //需要特殊处理抽象方法的默认参数 eg: abstract void SomeMethod(SomeType? para = null);
            var ignoreDefault = node.Default != null &&
                                node.Parent?.Parent is MethodDeclarationSyntax methodDeclaration &&
                                methodDeclaration.HasAbstractModifier();
            if (!ToJavaScript && ignoreDefault &&
                node.Default!.Value is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.NullLiteralExpression)
                Write('?');

            if (!ToJavaScript && node.Type != null)
            {
                Write(": ");
                //如果是ref参数，转换为System.Ref
                var isRef = node.Modifiers.Any(m => m.Kind() == SyntaxKind.RefKeyword);
                if (isRef)
                {
                    AddUsedModule("System");
                    Write("System.Ref<");
                }
                Visit(node.Type);
                if (isRef)
                    Write('>');
            }

            if (node.Default != null && !ignoreDefault)
                Visit(node.Default);
        }
    }
    
}