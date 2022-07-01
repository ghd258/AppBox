using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PixUI.CS2TS
{
    internal sealed class BlockEmitter : SyntaxEmitter<BlockSyntax>
    {
        internal static readonly BlockEmitter Default = new();

        private readonly string? _injectFirstStatement = null;
        private readonly bool _forSwitchPattern = false;

        private BlockEmitter() { }

        internal BlockEmitter(string injectFirstStatement)
        {
            _injectFirstStatement = injectFirstStatement;
        }

        internal BlockEmitter(bool forSwitchPattern)
        {
            _forSwitchPattern = forSwitchPattern;
        }

        internal override void Emit(Emitter emitter, BlockSyntax node)
        {
            //进入Block
            emitter.EnterBlock(node);

            if (!_forSwitchPattern)
                emitter.VisitToken(node.OpenBraceToken);

            if (_injectFirstStatement != null)
            {
                emitter.WriteLeadingWhitespaceOnly(node);
                emitter.Write('\t');
                emitter.Write(_injectFirstStatement);
                emitter.Write('\n');

                emitter.InjectIsPatternExpression = null; //must reset it
            }

            var statements = node.Statements;
            for (var i = 0; i < statements.Count; i++)
            {
                var statement = statements[i];
                if (i == statements.Count - 1 && _forSwitchPattern &&
                    statement is BreakStatementSyntax)
                    break;

                emitter.Visit(statement);
            }

            //离开block前需要处理相应的dispose
            emitter.LeaveBlock(statements.Count > 0
                               && statements.Last() is ReturnStatementSyntax);

            if (!_forSwitchPattern)
                emitter.VisitToken(node.CloseBraceToken);
        }
    }
}