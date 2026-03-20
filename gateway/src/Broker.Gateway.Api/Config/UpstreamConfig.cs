namespace Broker.Gateway.Api.Config;

public sealed class UpstreamEntry
{
    public string Address { get; set; } = string.Empty;
    public List<string> Routes { get; set; } = [];
}

public sealed class UpstreamsConfig
{
    public Dictionary<string, UpstreamEntry> Upstreams { get; set; } = new();
}
