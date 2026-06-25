using Dokkana.BLL.DTOs.Auth;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IAuthManager
    {
        Task<GeneralResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto);
        Task<GeneralResult<AuthResponseDTO>> LoginAsync(LoginDTO dto);
        Task<GeneralResult<AuthResponseDTO>> RefreshAsync(RefreshTokenDTO dto);
        Task<GeneralResult> RevokeRefreshTokenAsync(RefreshTokenDTO dto);
        Task<UserProfileDTO?> GetProfileAsync(string userId);
        Task<GeneralResult<UserProfileDTO>> UpdateProfileAsync(string userId, UserProfileUpdateDTO dto);
        Task<GeneralResult> ChangePasswordAsync(string userId, ChangePasswordDTO dto);
    }
}
