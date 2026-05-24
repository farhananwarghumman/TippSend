using System.Text.Json;

namespace TippSendApp.Services;

public class RuntimeSettings
{
    public bool OperatingAllDays { get; set; } = true;
    public int MinBookingNoticeHours { get; set; } = 2;
}

public class AppSettingsService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public AppSettingsService(IConfiguration config, IWebHostEnvironment env)
    {
        var dir = config["AppSettingsDir"] ?? env.ContentRootPath;
        _filePath = Path.Combine(dir, "runtime_settings.json");
    }

    public RuntimeSettings Get()
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
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null) Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _json));
    }
}
