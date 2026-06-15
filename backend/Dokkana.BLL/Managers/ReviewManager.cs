using Dokkana.BLL.DTOs.Engagement;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class ReviewManager : IReviewManager
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ProductReviewDTO>> GetByProductAsync(int productId, string? currentUserId)
        {
            var reviews = await _unitOfWork.ProductReviews.Query()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => Map(r, currentUserId)).ToList();
        }

        public Task<int> CountByUserAsync(string userId)
            => _unitOfWork.ProductReviews.CountAsync(r => r.UserId == userId);

        public async Task<GeneralResult<ProductReviewDTO>> CreateOrUpdateAsync(
            string userId,
            int productId,
            ProductReviewCreateDTO dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return GeneralResult<ProductReviewDTO>.Fail("Rating must be between 1 and 5.");

            var productExists = await _unitOfWork.Products.Query().AnyAsync(p => p.Id == productId);
            if (!productExists) return GeneralResult<ProductReviewDTO>.Fail("Product not found.");

            var review = await _unitOfWork.ProductReviews.Query()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

            if (review is null)
            {
                review = new ProductReview
                {
                    UserId = userId,
                    ProductId = productId,
                    Rating = dto.Rating,
                    Comment = Clean(dto.Comment)
                };
                await _unitOfWork.ProductReviews.AddAsync(review);
            }
            else
            {
                review.Rating = dto.Rating;
                review.Comment = Clean(dto.Comment);
                review.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ProductReviews.Update(review);
            }

            await _unitOfWork.SaveChangesAsync();

            var hydrated = await _unitOfWork.ProductReviews.Query()
                .Include(r => r.User)
                .FirstAsync(r => r.Id == review.Id);

            return GeneralResult<ProductReviewDTO>.Ok(Map(hydrated, userId), "Review saved.");
        }

        public async Task<GeneralResult> DeleteAsync(string userId, bool isAdmin, int reviewId)
        {
            var review = await _unitOfWork.ProductReviews.Query().FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review is null) return GeneralResult.Fail("Review not found.");
            if (!isAdmin && review.UserId != userId) return GeneralResult.Fail("You can only delete your own review.");

            _unitOfWork.ProductReviews.Delete(review);
            await _unitOfWork.SaveChangesAsync();
            return GeneralResult.Ok("Review deleted.");
        }

        private static ProductReviewDTO Map(ProductReview review, string? currentUserId) => new()
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserName = review.User is null ? "Dokkana shopper" : $"{review.User.FirstName} {review.User.LastName}".Trim(),
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            CanEdit = currentUserId is not null && review.UserId == currentUserId
        };

        private static string? Clean(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return null;
            return trimmed.Length <= 800 ? trimmed : trimmed[..800];
        }
    }
}
