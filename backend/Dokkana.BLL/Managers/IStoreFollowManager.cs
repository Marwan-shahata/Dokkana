using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IStoreFollowManager
    {
        Task<IReadOnlyList<FollowedStoreDTO>> GetAsync(string userId);
        Task<GeneralResult> FollowAsync(string userId, int storeId);
        Task<GeneralResult> UnfollowAsync(string userId, int storeId);
    }
}
