using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class StoreFollowManager : IStoreFollowManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoreFollowManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<FollowedStoreDTO>> GetAsync(string userId)
        {
            return await _unitOfWork.StoreFollows.Query()
                .Include(f => f.Store).ThenInclude(s => s!.Products)
                .Where(f => f.UserId == userId && f.Store != null)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FollowedStoreDTO
                {
                    StoreId = f.StoreId,
                    Name = f.Store!.Name,
                    Description = f.Store.Description,
                    LogoUrl = f.Store.LogoUrl,
                    Address = f.Store.Address,
                    ProductCount = f.Store.Products.Count,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<GeneralResult> FollowAsync(string userId, int storeId)
        {
            var storeExists = await _unitOfWork.Stores.Query().AnyAsync(s => s.Id == storeId && s.IsApproved);
            if (!storeExists) return GeneralResult.Fail("Store not found.");

            var exists = await _unitOfWork.StoreFollows.Query()
                .AnyAsync(f => f.UserId == userId && f.StoreId == storeId);
            if (exists) return GeneralResult.Ok("Store already saved.");

            await _unitOfWork.StoreFollows.AddAsync(new StoreFollow
            {
                UserId = userId,
                StoreId = storeId
            });
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Store saved.");
        }

        public async Task<GeneralResult> UnfollowAsync(string userId, int storeId)
        {
            var follow = await _unitOfWork.StoreFollows.Query()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.StoreId == storeId);
            if (follow is null) return GeneralResult.Fail("Saved store not found.");

            _unitOfWork.StoreFollows.Delete(follow);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Store removed.");
        }
    }
}
