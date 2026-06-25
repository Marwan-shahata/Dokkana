namespace Dokkana.BLL.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Saves an uploaded file to wwwroot/uploads/{folder}/{guid}.{ext}.
        /// Returns the relative public URL (e.g. "/uploads/products/abc.jpg").
        /// </summary>
        Task<string> UploadAsync(Stream fileStream, string fileName, string folder);

        /// <summary>
        /// Validates extension, declared content type, and image signature.
        /// Resets stream position when the stream supports seeking.
        /// </summary>
        Task<bool> IsValidImageAsync(Stream fileStream, string fileName, string? contentType);

        /// <summary>
        /// Maps a stored upload URL (e.g. "/uploads/products/x.jpg" or an absolute
        /// URL containing that path) back to the local file and returns it as a
        /// base64 "data:" URL for multimodal AI calls. Returns null when the URL is
        /// not a local upload or the file is missing/unsupported.
        /// </summary>
        Task<string?> TryReadImageAsDataUrlAsync(string? url, CancellationToken cancellationToken = default);
    }
}
