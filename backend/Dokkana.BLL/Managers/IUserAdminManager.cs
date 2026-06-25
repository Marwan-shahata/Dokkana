using Dokkana.BLL.DTOs.User;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    // Admin-facing user management (list + enable/disable).
    public interface IUserAdminManager
    {
        Task<IEnumerable<UserAdminDTO>> GetAllAsync();
        Task<GeneralResult> SetDisabledAsync(string userId, bool disabled);
    }
}
