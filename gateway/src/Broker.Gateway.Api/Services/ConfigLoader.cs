using Broker.Gateway.Api.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Broker.Gateway.Api.Services;

public sealed class ConfigLoader
{
    private readonly string _configDir;
    private readonly ILogger<ConfigLoader> _logger;
    private readonly IDeserializer _yaml;
    private readonly ISerializer _yamlSerializer;
    private FileSystemWatcher? _watcher;

    private MenuConfig _menu = new();
    private EntitiesConfig _entities = new();
    private UpstreamsConfig _upstreams = new();

    public MenuConfig Menu => _menu;
    public EntitiesConfig Entities => _entities;
    public UpstreamsConfig Upstreams => _upstreams;

    public ConfigLoader(IConfiguration configuration, ILogger<ConfigLoader> logger)
    {
        _configDir = configuration["ConfigDir"] ?? Path.Combine(AppContext.BaseDirectory, "config");
        _logger = logger;
        _yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        Load();
        StartWatcher();
    }

    public void Load()
    {
        _menu = LoadFile<MenuConfig>("menu.yaml") ?? new();
        _entities = LoadFile<EntitiesConfig>("entities.yaml") ?? new();
        _upstreams = LoadFile<UpstreamsConfig>("upstreams.yaml") ?? new();
        _logger.LogInformation(
            "Config loaded: {MenuItems} menu items, {Entities} entities, {Upstreams} upstreams",
            _menu.Menu.Count, _entities.Entities.Count, _upstreams.Upstreams.Count);
    }

    public void SaveMenu(MenuConfig config)
    {
        SaveFile("menu.yaml", config);
        _menu = config;
        _logger.LogInformation("Menu config saved: {Count} items", config.Menu.Count);
    }

    public void SaveEntities(EntitiesConfig config)
    {
        SaveFile("entities.yaml", config);
        _entities = config;
        _logger.LogInformation("Entities config saved: {Count} entities", config.Entities.Count);
    }

    public void SaveUpstreams(UpstreamsConfig config)
    {
        SaveFile("upstreams.yaml", config);
        _upstreams = config;
        _logger.LogInformation("Upstreams config saved: {Count} upstreams", config.Upstreams.Count);
    }

    private T? LoadFile<T>(string filename)
    {
        var path = Path.Combine(_configDir, filename);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Config file not found: {Path}", path);
            return default;
        }

        var content = File.ReadAllText(path);
        return _yaml.Deserialize<T>(content);
    }

    private void SaveFile<T>(string filename, T data)
    {
        var path = Path.Combine(_configDir, filename);
        var content = _yamlSerializer.Serialize(data);
        File.WriteAllText(path, content);
    }

    private void StartWatcher()
    {
        if (!Directory.Exists(_configDir)) return;

        _watcher = new FileSystemWatcher(_configDir, "*.yaml")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcher.Changed += OnConfigChanged;
        _watcher.Created += OnConfigChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Config file changed: {File}, reloading...", e.Name);
        try
        {
            // Debounce: file system may fire multiple events
            Thread.Sleep(200);
            Load();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload config after change to {File}", e.Name);
        }
    }
}
