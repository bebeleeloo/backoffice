namespace Broker.Backoffice.Application.Common;

public abstract record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
    public string? Q { get; init; }
}
