using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Broker.Gateway.Api.Services;

/// <summary>
/// Builds YARP proxy configuration from upstreams.yaml.
/// Each upstream route prefix becomes a YARP route + cluster.
/// </summary>
public sealed class YamlProxyConfigProvider : IProxyConfigProvider
{
    private volatile YamlProxyConfig _config;

    public YamlProxyConfigProvider(ConfigLoader configLoader)
    {
        _config = BuildConfig(configLoader);
    }

    public IProxyConfig GetConfig() => _config;

    private static YamlProxyConfig BuildConfig(ConfigLoader configLoader)
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

        return new YamlProxyConfig(routes, clusters);
    }

    private sealed class YamlProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters) : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes => routes;
        public IReadOnlyList<ClusterConfig> Clusters => clusters;
        public IChangeToken ChangeToken { get; } = new CancellationChangeToken(CancellationToken.None);
    }
}
