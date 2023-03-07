using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PixUI.CS2TS
{
    partial class Emitter
    {
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.HasAbstractModifier() || node.Parent is InterfaceDeclarationSyntax)
            {
                EmitAbstractProperty(node);
            }
            else if (IsAutoProperty(node, out var hasDifferentModifier, out var isReadonly))
            {
                if (hasDifferentModifier) //eg: public string Name {get; private set;}
                    EmitAutoProperty(node, false);
                else if (isReadonly) //eg: public string Name {get;}
                    EmitReadonlyAutoProperty(node);
                else
                    EmitAutoPropertyToField(node);
            }
            else
            {
                EmitGetterSetter(node);
            }
        }

        private static bool IsAutoProperty(PropertyDeclarationSyntax node,
            out bool hasDifferentModifier, out bool isReadonly)
        {
            hasDifferentModifier = false;
            isReadonly = node.ExpressionBody != null;
            if (node.AccessorList == null) return true;

            var hasModifier = false;
            var hasSetter = false;
            foreach (var item in node.AccessorList.Accessors)
            {
                if (item.Modifiers.Any())
                    hasModifier = true;
                if (item.Keyword.Text == "set")
                    hasSetter = true;

                if (item.Body != null || item.ExpressionBody != null)
                {
                    hasDifferentModifier = hasModifier;
                    return false;
                }
            }

            isReadonly = !hasSetter;
            hasDifferentModifier = hasModifier;
            return true;
        }

        private void EmitPropertyField(PropertyDeclarationSyntax node, string fieldName)
        {
            Write(fieldName);
            if (node.Initializer != null && node.Initializer.Value.Kind() ==
                SyntaxKind.SuppressNullableWarningExpression)
            {
                //definite assignment assertion
                if (!node.HasStaticModifier()) //排除static
                    Write('!');
            }

            Write(": ");
            Visit(node.Type);

            if (node.Initializer != null)
            {
                if (node.Initializer.Value.Kind() != SyntaxKind.SuppressNullableWarningExpression)
                {
                    Write(" = ");
                    Visit(node.Initializer.Value);
                }
            }
            else
            {
                TryWriteDefaultValueForValueType(node.Type, node);
            }
        }

        private void EmitAutoPropertyToField(PropertyDeclarationSyntax node)
        {
            WriteLeadingTrivia(node);
            WriteModifiers(node.Modifiers);

            EmitPropertyField(node, node.Identifier.Text);

            Write(';');
            WriteTrailingTrivia(node);
        }

        private void EmitReadonlyAutoProperty(PropertyDeclarationSyntax node)
        {
            // eg: public string Name {get;}
            if (node.Initializer == null && node.ExpressionBody == null)
            {
                EmitAutoProperty(node, true);
                return;
            }

            // eg: public string Name {get;} => "Hello";
            WriteLeadingTrivia(node);
            WriteModifiers(node.Modifiers);

            Write("get ");
            Write(node.Identifier.Text);
            Write("(): ");
            Visit(node.Type);
            Write(" {");

            if (node.Initializer != null)
            {
                if (node.Initializer.Value.Kind() != SyntaxKind.SuppressNullableWarningExpression)
                {
                    Write(" return ");
                    Visit(node.Initializer.Value);
                }
                else
                {
                    Write(" throw new Error()");
                }
            }
            else if (node.ExpressionBody != null)
            {
                Write(" return ");
                Visit(node.ExpressionBody.Expression);
            }

            Write("; }");
            WriteTrailingTrivia(node);
        }

        private void EmitAutoProperty(PropertyDeclarationSyntax node, bool forGetOnly)
        {
            var fieldName = $"#{node.Identifier.Text}";

            var isStatic = node.HasStaticModifier();

            WriteLeadingTrivia(node);
            if (isStatic)
                Write("static ");
            EmitPropertyField(node, fieldName);
            Write(";\n");

            foreach (var item in node.AccessorList!.Accessors)
            {
                WriteLeadingWhitespaceOnly(node);
                if (item.Modifiers.Any())
                {
                    WriteModifiers(item.Modifiers);
                    if (isStatic)
                        Write("static ");
                }
                else
                {
                    WriteModifiers(node.Modifiers);
                }

                if (item.Keyword.Kind() == SyntaxKind.GetKeyword)
                    EmitAutoPropertyGetter(node, isStatic, fieldName);
                else
                    EmitAutoPropertySetter(node, isStatic, fieldName);
            }

            if (forGetOnly)
            {
                WriteLeadingWhitespaceOnly(node);
                Write("private ");
                if (isStatic)
                    Write("static ");
                EmitAutoPropertySetter(node, isStatic, fieldName);
            }

            WriteTrailingTrivia(node);
        }

        private void EmitAutoPropertyGetter(PropertyDeclarationSyntax node, bool isStatic, string fieldName)
        {
            Write("get ");
            Write(node.Identifier.Text);
            Write("() { return ");
            Write(isStatic ? node.GetTypeDeclaration()!.Identifier.Text : "this");
            Write('.');
            Write(fieldName);
            Write("; }\n");
        }

        private void EmitAutoPropertySetter(PropertyDeclarationSyntax node, bool isStatic, string fieldName)
        {
            Write("set ");
            Write(node.Identifier.Text);
            Write("(value) {");
            Write(isStatic ? node.GetTypeDeclaration()!.Identifier.Text : "this");
            Write('.');
            Write(fieldName);
            Write(" = value; }");
        }

        private void EmitGetterSetter(PropertyDeclarationSyntax node)
        {
            foreach (var item in node.AccessorList!.Accessors)
            {
                if (item.Keyword.Kind() == SyntaxKind.GetKeyword)
                {
                    WriteLeadingTrivia(node);
                    WriteModifiers(item.Modifiers.Any() ? item.Modifiers : node.Modifiers);

                    Write("get ");
                    Write(node.Identifier.Text);
                    Write("(): ");
                    Visit(node.Type);
                    if (item.ExpressionBody != null)
                    {
                        Write(" { return ");
                        Visit(item.ExpressionBody.Expression);
                        Write("; }\n");
                    }
                    else
                    {
                        Write('\n');
                        Visit(item.Body);
                    }
                }
                else
                {
                    WriteLeadingWhitespaceOnly(node);
                    WriteModifiers(item.Modifiers.Any() ? item.Modifiers : node.Modifiers);

                    Write("set ");
                    Write(node.Identifier.Text);
                    Write("(value");
                    if (!ToJavaScript)
                    {
                        Write(": ");
                        Visit(node.Type);
                    }

                    Write(')');
                    if (item.ExpressionBody != null)
                    {
                        Write(" { ");
                        Visit(item.ExpressionBody.Expression);
                        Write("; }");
                    }
                    else
                    {
                        Write('\n');
                        Visit(item.Body);
                    }
                }
            }
        }

        private void EmitAbstractProperty(PropertyDeclarationSyntax node)
        {
            foreach (var item in node.AccessorList!.Accessors)
            {
                if (item.Keyword.Kind() == SyntaxKind.GetKeyword)
                {
                    WriteLeadingTrivia(node);
                    if (node.Parent is not InterfaceDeclarationSyntax)
                    {
                        WriteModifiers(item.Modifiers.Any()
                            ? item.Modifiers
                            : node.Modifiers);
                    }

                    Write("get ");
                    Write(node.Identifier.Text);
                    Write("(): ");
                    DisableVisitLeadingTrivia();
                    Visit(node.Type);
                    EnableVisitLeadingTrivia();
                    Write(";\n");
                }
                else
                {
                    WriteLeadingWhitespaceOnly(node);
                    if (node.Parent is not InterfaceDeclarationSyntax)
                    {
                        WriteModifiers(item.Modifiers.Any()
                            ? item.Modifiers
                            : node.Modifiers);
                    }

                    Write("set ");
                    Write(node.Identifier.Text);
                    Write("(value: ");
                    DisableVisitLeadingTrivia();
                    Visit(node.Type);
                    EnableVisitLeadingTrivia();
                    Write(");");
                }
            }

            WriteTrailingTrivia(node);
        }
    }
}