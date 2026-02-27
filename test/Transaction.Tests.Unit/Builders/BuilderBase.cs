using System.Linq.Expressions;
using System.Reflection;

namespace Transaction.Tests.Unit.Builders;

public abstract class BuilderBase<T>(T item) where T : class
{
    protected T _item = item;

    public T Build() => _item;

    protected BuilderBase<T> With<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            PropertyInfo property = (PropertyInfo)memberExpression.Member;
            property.SetValue(_item, value);
        }

        return this;
    }
}
