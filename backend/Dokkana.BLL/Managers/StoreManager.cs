using Dokkana.BLL.DTOs.Store;
using Dokkana.Common;
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
            // Project with the live product count so list views don't show 0.
            return await _unitOfWork.Stores.Query()
                .OrderBy(s => s.Name)
                .Select(ToDtoWithCount)
                .ToListAsync();
        }

        public async Task<IEnumerable<StoreDTO>> GetApprovedAsync()
        {
            return await _unitOfWork.Stores.Query()
                .Where(s => s.IsApproved)
                .OrderBy(s => s.Name)
                .Select(ToDtoWithCount)
                .ToListAsync();
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
                Tier = store.Tier == StoreTier.Pro ? "Pro" : "Free",
                SubscriptionStatus = store.SubscriptionStatus,
                SubscriptionRenewsAt = store.SubscriptionRenewsAt,
                WalletBalance = store.WalletBalance,
                WhatsAppPhone = store.WhatsAppPhone,
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
                LogoUrl = dto.LogoUrl,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                WhatsAppPhone = dto.WhatsAppPhone,
                OwnerId = ownerId,
                IsApproved = false
            };

            await _unitOfWork.Stores.AddAsync(store);
            await _unitOfWork.SaveChangesAsync();
            return Map(store);
        }

        public async Task<OwnershipResult> UpdateAsync(int id, StoreUpdateDTO dto, string userId, bool isAdmin)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store is null) return OwnershipResult.NotFound;

            // Security: a StoreOwner may only edit their own store.
            if (!isAdmin && store.OwnerId != userId) return OwnershipResult.Forbidden;

            if (dto.Name is not null) store.Name = dto.Name;
            if (dto.Description is not null) store.Description = dto.Description;
            if (dto.LogoUrl is not null) store.LogoUrl = dto.LogoUrl;
            if (dto.Address is not null) store.Address = dto.Address;
            if (dto.Latitude.HasValue) store.Latitude = dto.Latitude;
            if (dto.Longitude.HasValue) store.Longitude = dto.Longitude;
            if (dto.WhatsAppPhone is not null) store.WhatsAppPhone = dto.WhatsAppPhone;

            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();
            return OwnershipResult.Success;
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

        public async Task<OwnershipResult> SetTierAsync(int id, StoreTier tier, string userId, bool isAdmin)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store is null) return OwnershipResult.NotFound;

            // Security: a StoreOwner may only change their own store's plan.
            if (!isAdmin && store.OwnerId != userId) return OwnershipResult.Forbidden;

            store.Tier = tier;
            store.SubscriptionStatus = tier == StoreTier.Pro ? "Active" : "Free";
            store.SubscriptionRenewsAt = tier == StoreTier.Pro ? DateTime.UtcNow.AddMonths(1) : null;
            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();
            return OwnershipResult.Success;
        }

        // EF-translatable projection that includes the live product count, used by
        // the list endpoints so store rows show a real count instead of 0.
        private static readonly System.Linq.Expressions.Expression<Func<Store, StoreDTO>> ToDtoWithCount =
            s => new StoreDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                LogoUrl = s.LogoUrl,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                IsApproved = s.IsApproved,
                Tier = s.Tier == StoreTier.Pro ? "Pro" : "Free",
                SubscriptionStatus = s.SubscriptionStatus,
                SubscriptionRenewsAt = s.SubscriptionRenewsAt,
                WalletBalance = s.WalletBalance,
                WhatsAppPhone = s.WhatsAppPhone,
                ProductCount = s.Products.Count
            };

        // Manual mapping (keeping the same style as the existing codebase - no AutoMapper).
        private static StoreDTO Map(Store s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            LogoUrl = s.LogoUrl,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            IsApproved = s.IsApproved,
            Tier = s.Tier == StoreTier.Pro ? "Pro" : "Free",
            SubscriptionStatus = s.SubscriptionStatus,
            SubscriptionRenewsAt = s.SubscriptionRenewsAt,
            WalletBalance = s.WalletBalance,
            WhatsAppPhone = s.WhatsAppPhone
        };
    }
}
