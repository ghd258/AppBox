using System;
using System.Collections.Generic;
using System.Text;
using AppBoxCore;

namespace AppBoxStore;

/// <summary>
/// 子查询
/// 用于将外部查询[OutQuery]或查询[Query]包装为子查询
/// </summary>
public sealed class SqlSubQuery : Expression, ISqlQueryJoin
{
    internal SqlSubQuery(ISqlSelectQuery target)
    {
        Target = target;
    }

    private IList<SqlJoin>? _joins;

    public ISqlSelectQuery Target { get; }

    public IList<SqlSelectItemExpression> T => Target.Selects!;

    public bool HasJoins => _joins != null && _joins.Count > 0;

    public IList<SqlJoin> Joins => _joins ??= new List<SqlJoin>();

    public override ExpressionType Type => ExpressionType.SubQueryExpression;

    public override void ToCode(StringBuilder sb, string? preTabs)
    {
        sb.AppendFormat("SubQuery({0})", Target); //TODO: fix target.ToCode
    }
}