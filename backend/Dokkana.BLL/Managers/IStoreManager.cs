using Dokkana.BLL.DTOs.Store;

namespace Dokkana.BLL.Managers
{
    public interface IStoreManager
    {
        Task<IEnumerable<StoreDTO>> GetAllAsync();
        Task<StoreDTO?> GetByIdAsync(int id);
        Task<StoreDTO> CreateAsync(StoreCreateDTO dto, string ownerId);
        Task<bool> ApproveAsync(int id);
    }
}
