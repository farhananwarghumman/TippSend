using System.Text.Json;

namespace TippSendApp.Services;

public class RuntimeSettings
{
    public bool OperatingAllDays { get; set; } = false;
    public int MinBookingNoticeHours { get; set; } = 2;
}

public class AppSettingsService
{
    private readonly string _filePath;
    private readonly OperationalStore? _store;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public AppSettingsService(IConfiguration config, IWebHostEnvironment env, OperationalStore? store = null)
    {
        _store = env.IsDevelopment() && config.GetValue<bool>("LocalPreview") ? null : store;
        var dir = config["AppSettingsDir"] ?? env.ContentRootPath;
        _filePath = Path.Combine(dir, "runtime_settings.json");
    }

    public RuntimeSettings Get()
        => _store is null ? Initial() : _store.Execute("runtime-settings",Initial,s=>s);
    private RuntimeSettings Initial()
    {
        try
        {
            if (File.Exists(_filePath))
                return JsonSerializer.Deserialize<RuntimeSettings>(File.ReadAllText(_filePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save(RuntimeSettings settings)
    {
        if(settings.MinBookingNoticeHours is <0 or >168) throw new InvalidOperationException("Booking notice must be between 0 and 168 hours.");
        if(_store is not null) { _store.Execute("runtime-settings",Initial,s=>{s.OperatingAllDays=settings.OperatingAllDays;s.MinBookingNoticeHours=settings.MinBookingNoticeHours;return true;},true);return; }
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null) Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _json));
    }
}
