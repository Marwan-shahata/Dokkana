using Dokkana.BLL.Services;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadsController : ControllerBase
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private readonly IFileService _files;

        public UploadsController(IFileService files)
        {
            _files = files;
        }

        [HttpPost("images")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "products")
        {
            if (file.Length == 0) return BadRequest(GeneralResult.Fail("Upload a non-empty image file."));
            if (file.Length > MaxImageBytes) return BadRequest(GeneralResult.Fail("Image must be 5 MB or smaller."));
            if (!_files.IsValidImage(file.FileName)) return BadRequest(GeneralResult.Fail("Use JPG, PNG, GIF, or WEBP."));

            folder = string.IsNullOrWhiteSpace(folder) ? "products" : folder.Trim().ToLowerInvariant();
            if (folder is not ("products" or "stores" or "avatars" or "categories"))
                return BadRequest(GeneralResult.Fail("Invalid upload folder."));

            await using var stream = file.OpenReadStream();
            var url = await _files.UploadAsync(stream, file.FileName, folder);

            return Ok(GeneralResult<object>.Ok(new { url }, "Image uploaded."));
        }
    }
}
