using Dokkana.BLL.DTOs.Store;

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
        Task<bool> UpdateAsync(int id, StoreUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ApproveAsync(int id);
    }
}

