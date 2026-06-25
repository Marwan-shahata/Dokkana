using Dokkana.BLL.DTOs.Store;
using Dokkana.Common;
using Dokkana.DAL.Models;

namespace Dokkana.BLL.Managers
{
    public interface IStoreManager
    {
        Task<IEnumerable<StoreDTO>> GetAllAsync();
        Task<IEnumerable<StoreDTO>> GetApprovedAsync();
        Task<StoreDTO?> GetByIdAsync(int id);
        Task<StoreDetailsDTO?> GetDetailsAsync(int id);
        Task<StoreDTO?> GetByOwnerAsync(string ownerId);
        Task<StoreDTO> CreateAsync(StoreCreateDTO dto, string ownerId);

        // Ownership-guarded. Admins bypass the owner check.
        Task<OwnershipResult> UpdateAsync(int id, StoreUpdateDTO dto, string userId, bool isAdmin);
        Task<bool> DeleteAsync(int id);
        Task<bool> ApproveAsync(int id);

        // Ownership-guarded plan change (Free / Pro). Admins bypass the owner check.
        Task<OwnershipResult> SetTierAsync(int id, StoreTier tier, string userId, bool isAdmin);
    }
}
