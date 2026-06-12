using Dokkana.BLL.DTOs.Store;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IEnumerable<StoreDTO>> GetApprovedAsync()
        {
            var stores = await _unitOfWork.Stores.FindAsync(s => s.IsApproved);
            return stores.Select(Map);
        }

        public async Task<StoreDTO?> GetByIdAsync(int id)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            return store is null ? null : Map(store);
        }

        public async Task<StoreDetailsDTO?> GetDetailsAsync(int id)
        {
            var store = await _unitOfWork.Stores.Query()
                .Include(s => s.Owner)
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store is null) return null;

            return new StoreDetailsDTO
            {
                Id = store.Id,
                Name = store.Name,
                Description = store.Description,
                LogoUrl = store.LogoUrl,
                Address = store.Address,
                Latitude = store.Latitude,
                Longitude = store.Longitude,
                IsApproved = store.IsApproved,
                OwnerName = store.Owner is not null
                    ? $"{store.Owner.FirstName} {store.Owner.LastName}"
                    : null,
                ProductCount = store.Products.Count
            };
        }

        public async Task<StoreDTO?> GetByOwnerAsync(string ownerId)
        {
            var stores = await _unitOfWork.Stores.FindAsync(s => s.OwnerId == ownerId);
            var store = stores.FirstOrDefault();
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

        public async Task<bool> UpdateAsync(int id, StoreUpdateDTO dto)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store is null) return false;

            if (dto.Name is not null) store.Name = dto.Name;
            if (dto.Description is not null) store.Description = dto.Description;
            if (dto.LogoUrl is not null) store.LogoUrl = dto.LogoUrl;
            if (dto.Address is not null) store.Address = dto.Address;
            if (dto.Latitude.HasValue) store.Latitude = dto.Latitude;
            if (dto.Longitude.HasValue) store.Longitude = dto.Longitude;

            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store is null) return false;

            _unitOfWork.Stores.Delete(store);
            await _unitOfWork.SaveChangesAsync();
            return true;
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

