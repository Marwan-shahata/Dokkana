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

        public FileService(IWebHostEnvironment env)
        {
            _webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string folder)
        {
            var extension = Path.GetExtension(fileName);
            var uniqueName = $"{Guid.NewGuid()}{extension}";

            var uploadsDir = Path.Combine(_webRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, uniqueName);

            using var output = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            // Return the relative URL for static file serving.
            return $"/uploads/{folder}/{uniqueName}";
        }

        public bool IsValidImage(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
        }
    }
}
