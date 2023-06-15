using System.Collections;
using System.Text;

namespace AppBoxCore;

public abstract class Expression
{
    public abstract ExpressionType Type { get; }

    #region ====Overrides====

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override string ToString()
    {
        var sb = StringBuilderCache.Acquire();
        ToCode(sb, null);
        return StringBuilderCache.GetStringAndRelease(sb);
    }

    /// <summary>
    /// 转换为用于表达式编辑器的代码
    /// </summary>
    public abstract void ToCode(StringBuilder sb, string? preTabs);

    //TODO:待重构实现Emit il后移除
    //public abstract System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx);

    //TODO:直接参考FastExpressionCompiler emit code

    #endregion

    #region ====特定类型方法====

    public Expression Contains(Expression value) => new BinaryExpression(this, value, BinaryOperatorType.Like);

    /// <summary>
    /// 相当于expA = expB, 因无法重写=操作符
    /// </summary>
    public Expression Assign(Expression value) => new BinaryExpression(this, value, BinaryOperatorType.Assign);

    public BinaryExpression In(Expression list) => new(this, list, BinaryOperatorType.In);

    public BinaryExpression In(IEnumerable list) => new(this, new PrimitiveExpression(list), BinaryOperatorType.In);

    public BinaryExpression NotIn(Expression list) => new(this, list, BinaryOperatorType.NotIn);

    public BinaryExpression NotIn(IEnumerable list) =>
        new(this, new PrimitiveExpression(list), BinaryOperatorType.NotIn);

    #endregion

    #region ====操作符重载====

    public static Expression operator +(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Plus);
    }

    public static Expression operator -(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Minus);
    }

    public static Expression operator *(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Multiply);
    }

    public static Expression operator /(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Divide);
    }

    public static Expression operator %(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Modulo);
    }

    public static Expression operator &(Expression left, Expression right)
    {
        //暂转换为AndAlso
        return new BinaryExpression(left, right, BinaryOperatorType.AndAlso);
    }

    public static Expression operator |(Expression left, Expression right)
    {
        //暂转换为OrElse
        return new BinaryExpression(left, right, BinaryOperatorType.OrElse);
    }

    public static BinaryExpression operator ==(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Equal);
    }

    public static BinaryExpression operator !=(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.NotEqual);
    }

    public static BinaryExpression operator >(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Greater);
    }

    public static BinaryExpression operator >=(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.GreaterOrEqual);
    }

    public static BinaryExpression operator <(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.Less);
    }

    public static BinaryExpression operator <=(Expression left, Expression right)
    {
        return new BinaryExpression(left, right, BinaryOperatorType.LessOrEqual);
    }

    //public static BinaryExpression Op_Like(Expression left, Expression right)
    //{
    //    return new BinaryExpression(left, right, BinaryOperatorType.Like);
    //}

    #endregion

    #region ====隐式转换====

    public static implicit operator Expression(byte[] val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(bool val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(bool? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(char val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(char? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(DateTime val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(DateTime? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(decimal val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(decimal? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(Guid val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(Guid? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(int val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(int? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(string val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(float val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(float? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    public static implicit operator Expression(double val)
    {
        return new PrimitiveExpression(val);
    }

    public static implicit operator Expression(double? val)
    {
        return val.HasValue ? new PrimitiveExpression(val.Value) : new PrimitiveExpression(null);
    }

    #endregion

    #region ====Static Help Methods====

    public static bool IsNull(Expression? exp) => Equals(exp, null);

    #endregion
}