using Dokkana.BLL.DTOs.Store;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;

namespace Dokkana.BLL.Managers
{
    public class StoreManager : IStoreManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoreManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<StoreDTO>> GetAllAsync()
        {
            var stores = await _unitOfWork.Stores.GetAllAsync();
            return stores.Select(Map);
        }

        public async Task<StoreDTO?> GetByIdAsync(int id)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            return store is null ? null : Map(store);
        }

        public async Task<StoreDTO> CreateAsync(StoreCreateDTO dto, string ownerId)
        {
            var store = new Store
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                OwnerId = ownerId,
                IsApproved = false
            };

            await _unitOfWork.Stores.AddAsync(store);
            await _unitOfWork.SaveChangesAsync();
            return Map(store);
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store is null) return false;

            store.IsApproved = true;
            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Manual mapping (keeping the same style as the existing codebase — no AutoMapper).
        private static StoreDTO Map(Store s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            IsApproved = s.IsApproved
        };
    }
}
