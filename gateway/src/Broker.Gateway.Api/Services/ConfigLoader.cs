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
    private readonly object _lock = new();
    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _debounceCts;

    private volatile MenuConfig _menu = new();
    private volatile EntitiesConfig _entities = new();
    private volatile UpstreamsConfig _upstreams = new();

    /// <summary>
    /// Raised after upstreams configuration changes (via <see cref="Load"/>, <see cref="SaveUpstreams"/>, or file watcher reload).
    /// Subscribe to this event to trigger YARP proxy config reload.
    /// </summary>
    public event Action? OnUpstreamsChanged;

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

        try
        {
            Load();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration on startup, using empty defaults");
            _menu = new();
            _entities = new();
            _upstreams = new();
        }

        StartWatcher();
    }

    public void Load()
    {
        lock (_lock)
        {
            _menu = LoadFile<MenuConfig>("menu.yaml") ?? new();
            _entities = LoadFile<EntitiesConfig>("entities.yaml") ?? new();
            _upstreams = LoadFile<UpstreamsConfig>("upstreams.yaml") ?? new();
            _logger.LogInformation(
                "Config loaded: {MenuItems} menu items, {Entities} entities, {Upstreams} upstreams",
                _menu.Menu.Count, _entities.Entities.Count, _upstreams.Upstreams.Count);
        }

        OnUpstreamsChanged?.Invoke();
    }

    public void SaveMenu(MenuConfig config)
    {
        lock (_lock)
        {
            SaveFile("menu.yaml", config);
            _menu = config;
            _logger.LogInformation("Menu config saved: {Count} items", config.Menu.Count);
        }
    }

    public void SaveEntities(EntitiesConfig config)
    {
        lock (_lock)
        {
            SaveFile("entities.yaml", config);
            _entities = config;
            _logger.LogInformation("Entities config saved: {Count} entities", config.Entities.Count);
        }
    }

    public void SaveUpstreams(UpstreamsConfig config)
    {
        lock (_lock)
        {
            SaveFile("upstreams.yaml", config);
            _upstreams = config;
            _logger.LogInformation("Upstreams config saved: {Count} upstreams", config.Upstreams.Count);
        }

        OnUpstreamsChanged?.Invoke();
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
        var tempPath = path + ".tmp";

        try
        {
            var content = _yamlSerializer.Serialize(data);
            File.WriteAllText(tempPath, content);

            if (File.Exists(path))
            {
                var backupPath = path + ".bak";
                File.Copy(path, backupPath, overwrite: true);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath)) try { File.Delete(tempPath); } catch { }
            _logger.LogError(ex, "Failed to save config file: {Path}", path);
            throw;
        }
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

        // Debounce: file system may fire multiple events
        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        Task.Delay(300, cts.Token).ContinueWith(_ =>
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload config after change to {File}", e.Name);
            }
        }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }
}
