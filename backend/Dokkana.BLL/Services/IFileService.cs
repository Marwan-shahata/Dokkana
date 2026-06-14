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
        /// Validates file extension is an allowed image type.
        /// </summary>
        bool IsValidImage(string fileName);
    }
}
