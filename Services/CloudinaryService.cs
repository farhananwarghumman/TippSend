using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace TippSendApp.Services;

public class CloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly string _uploadsDirectory;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        _uploadsDirectory = config["UploadsDir"] ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads");
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
        if (file.Length is <= 0 or > 8_000_000) throw new InvalidOperationException("Upload an image smaller than 8 MB.");
        var extension=Path.GetExtension(file.FileName).ToLowerInvariant();
        await using(var input=file.OpenReadStream()) {
            var bytes=new byte[12]; var length=await input.ReadAsync(bytes);
            var jpeg=length>=3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255;
            var png=length>=8&&bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
            var webp=length>=12&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP";
            if (!(jpeg&&extension is ".jpg" or ".jpeg" || png&&extension==".png" || webp&&extension==".webp"))
                throw new InvalidOperationException("Upload a JPG, PNG or WebP image.");
        }
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
    private async Task<string?> SaveLocalAsync(IFormFile file)
    {
        var dir = _uploadsDirectory;
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(dir, name);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{name}";
    }
}
