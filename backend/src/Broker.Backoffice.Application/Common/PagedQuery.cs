namespace Broker.Backoffice.Application.Common;

public abstract record PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page { get => _page; init => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; init => _pageSize = value < 1 ? 1 : value > 10000 ? 10000 : value; }
    public string? Sort { get; init; }
    public string? Q { get; init; }
}
