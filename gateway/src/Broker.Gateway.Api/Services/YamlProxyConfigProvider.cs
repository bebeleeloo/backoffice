using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Broker.Gateway.Api.Services;

/// <summary>
/// Builds YARP proxy configuration from upstreams.yaml.
/// Each upstream route prefix becomes a YARP route + cluster.
/// When upstreams change, <see cref="Update"/> rebuilds the config and signals YARP to reload.
/// </summary>
public sealed class YamlProxyConfigProvider : IProxyConfigProvider
{
    private readonly ConfigLoader _configLoader;
    private readonly object _lock = new();
    private volatile YamlProxyConfig _config;
    private CancellationTokenSource _cts = new();

    public YamlProxyConfigProvider(ConfigLoader configLoader)
    {
        _configLoader = configLoader;
        _config = BuildConfig(configLoader, _cts);
    }

    public IProxyConfig GetConfig() => _config;

    /// <summary>
    /// Rebuilds proxy configuration from the current upstreams and signals YARP to pick up the changes.
    /// </summary>
    public void Update()
    {
        var newCts = new CancellationTokenSource();
        var newConfig = BuildConfig(_configLoader, newCts);

        CancellationTokenSource oldCts;
        lock (_lock)
        {
            oldCts = _cts;
            _cts = newCts;
            _config = newConfig;
        }

        try
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, safe to ignore
        }
    }

    private static YamlProxyConfig BuildConfig(ConfigLoader configLoader, CancellationTokenSource cts)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var (name, upstream) in configLoader.Upstreams.Upstreams)
        {
            // One cluster per upstream
            var cluster = new ClusterConfig
            {
                ClusterId = name,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    [name] = new() { Address = upstream.Address }
                }
            };
            clusters.Add(cluster);

            // One route per route prefix
            for (var i = 0; i < upstream.Routes.Count; i++)
            {
                var prefix = upstream.Routes[i];
                if (string.IsNullOrWhiteSpace(prefix)) continue;
                var routeId = $"{name}-{i}";
                var route = new RouteConfig
                {
                    RouteId = routeId,
                    ClusterId = name,
                    Match = new RouteMatch
                    {
                        Path = $"{prefix}/{{**catch-all}}"
                    }
                };
                routes.Add(route);

                // Also match the exact prefix (e.g., /api/v1/clients without trailing path)
                routes.Add(new RouteConfig
                {
                    RouteId = $"{routeId}-exact",
                    ClusterId = name,
                    Match = new RouteMatch
                    {
                        Path = prefix
                    }
                });
            }
        }

        return new YamlProxyConfig(routes, clusters, cts);
    }

    private sealed class YamlProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters,
        CancellationTokenSource cts) : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes => routes;
        public IReadOnlyList<ClusterConfig> Clusters => clusters;
        public IChangeToken ChangeToken { get; } = new CancellationChangeToken(cts.Token);
    }
}
