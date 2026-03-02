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

        bool desc;
        string prop;

        // Support "field asc" / "field desc" format (from frontend URL params)
        var parts = sort.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            prop = parts[0];
            desc = parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Legacy format: "-field" for descending, "field" for ascending
            desc = sort.StartsWith('-');
            prop = desc ? sort[1..] : sort;
        }

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
