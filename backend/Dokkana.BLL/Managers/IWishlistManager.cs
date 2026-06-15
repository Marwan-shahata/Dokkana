using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IWishlistManager
    {
        Task<IReadOnlyList<WishlistItemDTO>> GetAsync(string userId);
        Task<GeneralResult> AddAsync(string userId, int productId);
        Task<GeneralResult> RemoveAsync(string userId, int productId);
    }
}
