using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PixUI.CS2TS
{
    partial class Emitter
    {
        public override void VisitArrayType(ArrayTypeSyntax node)
        {
            if (node.ElementType is PredefinedTypeSyntax predefinedType)
            {
                var jsArrayType = GetJsNativeArrayType(node);
                if (jsArrayType != null)
                {
                    Write(jsArrayType);
                    return;
                }
            }

            Visit(node.ElementType);
            Write("[]");
        }

        internal static string? GetJsNativeArrayType(ArrayTypeSyntax node)
        {
            if (node.ElementType is PredefinedTypeSyntax predefinedType)
            {
                var jsArrayType = predefinedType.Keyword.Kind() switch
                {
                    SyntaxKind.ByteKeyword => "Uint8Array",
                    SyntaxKind.SByteKeyword => "Int8Array",
                    SyntaxKind.ShortKeyword => "Int16Array",
                    SyntaxKind.UShortKeyword => "Uint16Array",
                    SyntaxKind.CharKeyword => "Uint16Array",
                    SyntaxKind.IntKeyword => "Int32Array",
                    SyntaxKind.UIntKeyword => "Uint32Array",
                    SyntaxKind.FloatKeyword => "Float32Array",
                    SyntaxKind.DoubleKeyword => "Float64Array",
                    _ => null
                };
                return jsArrayType;
            }

            return null;
        }
    }
}