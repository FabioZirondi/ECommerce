using ECommerce.Shared.Results;

namespace ECommerce.API.Uploads;

public interface IImageStorage
{
    Task<Result<string>> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class LocalImageStorage : IImageStorage
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IWebHostEnvironment _environment;

    public LocalImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<Result<string>> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return Result<string>.Failure("Selecione uma imagem.");
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return Result<string>.Failure("A imagem deve ter no máximo 2 MB.");
        }

        if (!AllowedTypes.Contains(file.ContentType))
        {
            return Result<string>.Failure("Use JPG, PNG ou WebP.");
        }

        await using var input = file.OpenReadStream();
        if (!HasValidSignature(input, file.ContentType))
        {
            return Result<string>.Failure("O arquivo não é uma imagem válida.");
        }

        var extension = file.ContentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var folder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, fileName);

        await using var output = File.Create(path);
        input.Position = 0;
        await input.CopyToAsync(output, cancellationToken);

        return Result<string>.Success($"/uploads/{fileName}", 201);
    }

    private static bool HasValidSignature(Stream stream, string contentType)
    {
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            "image/webp" => read >= 12
                && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',
            _ => false
        };
    }
}
