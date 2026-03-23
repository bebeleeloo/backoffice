using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Orders.NonTradeOrders;
using Broker.Backoffice.Application.Orders.TradeOrders;
using Broker.Backoffice.Application.Transactions.NonTradeTransactions;
using Broker.Backoffice.Application.Transactions.TradeTransactions;

namespace Broker.Backoffice.Application.Common;

public sealed record EntityMetadataDto(string Name, List<string> Fields);

public static class EntityMetadataProvider
{
    private static readonly Dictionary<string, Type> DtoTypes = new()
    {
        ["Client"] = typeof(ClientListItemDto),
        ["Account"] = typeof(AccountListItemDto),
        ["Instrument"] = typeof(InstrumentListItemDto),
        ["TradeOrder"] = typeof(TradeOrderListItemDto),
        ["NonTradeOrder"] = typeof(NonTradeOrderListItemDto),
        ["TradeTransaction"] = typeof(TradeTransactionListItemDto),
        ["NonTradeTransaction"] = typeof(NonTradeTransactionListItemDto),
    };

    public static List<EntityMetadataDto> GetAll() =>
        DtoTypes.Select(kv => new EntityMetadataDto(
            kv.Key,
            kv.Value.GetProperties()
                .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
                .Order()
                .ToList()
        )).OrderBy(e => e.Name).ToList();
}
