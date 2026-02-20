namespace Broker.Backoffice.Domain.Countries;

public sealed class Country
{
    public Guid Id { get; init; }
    public string Iso2 { get; set; } = string.Empty;
    public string? Iso3 { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
