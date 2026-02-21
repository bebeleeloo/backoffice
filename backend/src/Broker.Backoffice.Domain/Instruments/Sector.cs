using System.Text.Json.Serialization;

namespace Broker.Backoffice.Domain.Instruments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Sector
{
    Technology = 0,
    Healthcare = 1,
    Finance = 2,
    Energy = 3,
    ConsumerDiscretionary = 4,
    ConsumerStaples = 5,
    Industrials = 6,
    Materials = 7,
    RealEstate = 8,
    Utilities = 9,
    Communication = 10,
    Other = 11
}
