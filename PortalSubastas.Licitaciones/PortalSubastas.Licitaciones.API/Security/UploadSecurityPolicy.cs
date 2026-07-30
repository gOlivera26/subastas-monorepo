using Microsoft.Extensions.Configuration;

namespace PortalSubastas.Licitaciones.API.Security;

public static class UploadSecurityPolicy
{
    public const long MaxDocumentBytes = 20 * 1024 * 1024;

    private static readonly string[] AllowedDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

    private static readonly string[] AllowedDocumentContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    public static async Task<string?> ValidateDocumentAsync(IFormFile? file, IConfiguration? configuration = null)
    {
        if (file is null || file.Length == 0)
        {
            return "El archivo es obligatorio.";
        }

        var maxBytes = configuration.GetUploadValue("Documents:MaxBytes", MaxDocumentBytes);
        if (file.Length > maxBytes)
        {
            return $"El archivo no puede superar los {BytesToMb(maxBytes)} MB.";
        }

        var allowedExtensions = configuration.GetUploadValues("Documents:AllowedExtensions", AllowedDocumentExtensions);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return "Tipo de archivo no permitido. Adjuntá PDF, JPG o PNG.";
        }

        var allowedContentTypes = configuration.GetUploadValues("Documents:AllowedContentTypes", AllowedDocumentContentTypes);
        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return "El tipo de contenido del archivo no es válido.";
        }

        if (!await HasValidMagicBytesAsync(file, extension))
        {
            return "El contenido del archivo no coincide con su extensión.";
        }

        return null;
    }

    public static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar, '_');
        }

        return safeName;
    }

    private static async Task<bool> HasValidMagicBytesAsync(IFormFile file, string extension)
    {
        var buffer = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        return extension switch
        {
            ".pdf" => read >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
            ".jpg" or ".jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => read >= 8 &&
                      buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                      buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A,
            _ => false
        };
    }

    private static long GetUploadValue(this IConfiguration? configuration, string key, long fallback)
    {
        var value = configuration?.GetValue<long?>($"Security:Uploads:{key}");
        return value.HasValue && value.Value > 0 ? value.Value : fallback;
    }

    private static string[] GetUploadValues(this IConfiguration? configuration, string key, string[] fallback)
    {
        var values = configuration?.GetSection($"Security:Uploads:{key}").Get<string[]>();
        return values is { Length: > 0 } ? values : fallback;
    }

    private static long BytesToMb(long bytes)
        => Math.Max(1, bytes / (1024 * 1024));
}
