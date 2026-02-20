using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Common;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public static IQueryable<T> SortBy<T>(
        this IQueryable<T> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return query;

        var desc = sort.StartsWith('-');
        var prop = desc ? sort[1..] : sort;

        var param = Expression.Parameter(typeof(T), "x");
        var member = typeof(T).GetProperty(prop,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (member is null) return query;

        var expr = Expression.Lambda(Expression.Property(param, member), param);
        var method = desc ? "OrderByDescending" : "OrderBy";
        var call = Expression.Call(typeof(Queryable), method,
            [typeof(T), member.PropertyType], query.Expression, Expression.Quote(expr));

        return query.Provider.CreateQuery<T>(call);
    }
}
