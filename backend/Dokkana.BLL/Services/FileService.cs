using Microsoft.AspNetCore.Hosting;

namespace Dokkana.BLL.Services
{
    public class FileService : IFileService
    {
        private readonly string _webRootPath;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };
        private static readonly Dictionary<string, string> ExtensionContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".gif"] = "image/gif",
                [".webp"] = "image/webp"
            };

        public FileService(IWebHostEnvironment env)
        {
            _webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{extension}";

            var uploadsDir = Path.Combine(_webRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, uniqueName);

            using var output = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            // Return the relative URL for static file serving.
            return $"/uploads/{folder}/{uniqueName}";
        }

        public async Task<string?> TryReadImageAsDataUrlAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // Locate the "/uploads/..." segment whether the URL is relative or absolute.
            const string marker = "/uploads/";
            var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var relative = url[(idx + 1)..].Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_webRootPath, relative);
            if (!File.Exists(filePath)) return null;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!ExtensionContentTypes.TryGetValue(extension, out var contentType)) return null;

            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            // Skip empty or oversized images (keep the request small).
            if (bytes.Length == 0 || bytes.Length > 8 * 1024 * 1024) return null;

            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }

        public async Task<bool> IsValidImageAsync(Stream fileStream, string fileName, string? contentType)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return false;

            if (!string.IsNullOrWhiteSpace(contentType) &&
                (!ExtensionContentTypes.TryGetValue(extension, out var expectedType) ||
                 !string.Equals(contentType, expectedType, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var header = new byte[12];
            var bytesRead = await fileStream.ReadAsync(header);
            if (fileStream.CanSeek) fileStream.Position = 0;

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                      header[0] == 0xFF &&
                                      header[1] == 0xD8 &&
                                      header[2] == 0xFF,
                ".png" => bytesRead >= 8 &&
                          header[0] == 0x89 &&
                          header[1] == 0x50 &&
                          header[2] == 0x4E &&
                          header[3] == 0x47 &&
                          header[4] == 0x0D &&
                          header[5] == 0x0A &&
                          header[6] == 0x1A &&
                          header[7] == 0x0A,
                ".gif" => bytesRead >= 6 &&
                          header[0] == 0x47 &&
                          header[1] == 0x49 &&
                          header[2] == 0x46 &&
                          header[3] == 0x38 &&
                          (header[4] == 0x37 || header[4] == 0x39) &&
                          header[5] == 0x61,
                ".webp" => bytesRead >= 12 &&
                           header[0] == 0x52 &&
                           header[1] == 0x49 &&
                           header[2] == 0x46 &&
                           header[3] == 0x46 &&
                           header[8] == 0x57 &&
                           header[9] == 0x45 &&
                           header[10] == 0x42 &&
                           header[11] == 0x50,
                _ => false
            };
        }
    }
}
