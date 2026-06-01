using Shared.Entities;
using System.Linq.Expressions;

namespace Database.Libraries.Helpers;

public static class LinqOrderByExtensions
{
    public static IQueryable<T> OrderByProperty<T>(
        this IQueryable<T> source,
        string propertyName,
        SortDirectionEnum direction)
    {
        return ApplyOrder(source, propertyName,
            direction == SortDirectionEnum.Ascending ? "OrderBy" : "OrderByDescending");
    }

    public static IQueryable<T> ThenByProperty<T>(
        this IQueryable<T> source,
        string propertyName,
        SortDirectionEnum direction)
    {
        return ApplyOrder(source, propertyName,
            direction == SortDirectionEnum.Ascending ? "ThenBy" : "ThenByDescending");
    }

    private static IQueryable<T> ApplyOrder<T>(
        IQueryable<T> source,
        string propertyName,
        string methodName)
    {
        var type = typeof(T);

        // Most important line: supports nested properties (e.g., "Customer.Name")
        Expression property = GetNestedPropertyExpression(typeof(T), propertyName, out ParameterExpression param);

        var delegateType = typeof(Func<,>).MakeGenericType(type, property.Type);
        var lambda = Expression.Lambda(delegateType, property, param);

        var result = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(type, property.Type)
            .Invoke(null, new object[] { source, lambda });

        return (IQueryable<T>)result!;
    }

    private static Expression GetNestedPropertyExpression(Type type, string propertyName, out ParameterExpression param)
    {
        param = Expression.Parameter(type, "x");

        Expression body = param;
        foreach (var member in propertyName.Split('.'))
        {
            body = Expression.PropertyOrField(body, member);
        }
        return body;
    }
}
