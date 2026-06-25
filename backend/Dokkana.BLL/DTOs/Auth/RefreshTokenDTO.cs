using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Auth
{
    public class RefreshTokenDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
