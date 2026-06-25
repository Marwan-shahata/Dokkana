using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IReviewManager
    {
        Task<IReadOnlyList<ProductReviewDTO>> GetByProductAsync(int productId, string? currentUserId);
        Task<int> CountByUserAsync(string userId);
        Task<GeneralResult<ProductReviewDTO>> CreateOrUpdateAsync(string userId, int productId, ProductReviewCreateDTO dto);
        Task<GeneralResult> DeleteAsync(string userId, bool isAdmin, int reviewId);
    }
}
