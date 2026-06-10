using Dokkana.BLL.DTOs.Auth;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IAuthManager
    {
        Task<GeneralResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto);
        Task<GeneralResult<AuthResponseDTO>> LoginAsync(LoginDTO dto);
    }
}
