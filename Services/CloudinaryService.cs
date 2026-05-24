using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace TippSendApp.Services;

public class CloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        var cloud = config["Cloudinary:CloudName"];
        var key = config["Cloudinary:ApiKey"];
        var secret = config["Cloudinary:ApiSecret"];
        if (!string.IsNullOrWhiteSpace(cloud) && !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(secret))
        {
            _cloudinary = new Cloudinary(new Account(cloud, key, secret)) { Api = { Secure = true } };
        }
    }

    public async Task<string?> UploadDeliveryPhotoAsync(IFormFile file, string orderReference)
    {
        if (_cloudinary is null)
            return await SaveLocalAsync(file);

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "tippsend/deliveries",
                PublicId = $"{orderReference}_{Guid.NewGuid():N}",
                Transformation = new Transformation()
                    .Quality("auto").FetchFormat("auto").Width(1400).Crop("limit")
            });

            if (result.Error is not null)
            {
                _logger.LogWarning("Cloudinary error: {Msg}", result.Error.Message);
                return await SaveLocalAsync(file);
            }

            return result.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload failed, falling back to local");
            return await SaveLocalAsync(file);
        }
    }

    // Local fallback — used when Cloudinary is not configured or fails
    private static async Task<string?> SaveLocalAsync(IFormFile file)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(dir, name);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{name}";
    }
}
