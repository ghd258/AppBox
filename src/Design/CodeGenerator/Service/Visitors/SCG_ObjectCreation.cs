using AppBoxCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AppBoxDesign;

internal partial class ServiceCodeGenerator
{
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbol = SemanticModel.GetSymbolInfo(node).Symbol;
        if (IsGenericCreate(symbol))
        {
            var typeArgs = symbol!.ContainingType.TypeArguments;
            var modelType = typeArgs[0];
            var modelNode = DesignHub.DesignTree.FindModelNodeByFullName(modelType.ToString())!;
            var model = (EntityModel)modelNode.Model;
            if (typeArgs.Length == 1)
            {
                return SyntaxFactory
                    .ParseExpression($"new {node.Type.ToString()}({model.Id.Value}L)")
                    .WithTriviaFrom(node);
            }

            //TODO: IndexScan有多个范型参数
            throw new NotImplementedException();
        }

        return base.VisitObjectCreationExpression(node);
    }

    private static bool IsGenericCreate(ISymbol? methodSymbol)
    {
        return methodSymbol != null &&
               methodSymbol.GetAttributes()
                   .Any(a => a.AttributeClass != null &&
                             a.AttributeClass.ToString() == "AppBoxStore.GenericCreateAttribute");
    }
}