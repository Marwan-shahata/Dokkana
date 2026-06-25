using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IMonetizationManager
    {
        Task<IReadOnlyList<PromotionDTO>> GetMerchantCampaignsAsync(string ownerId);
        Task<IReadOnlyList<PromotionDTO>> GetAdminCampaignsAsync(string? status = null);
        Task<(OwnershipResult Result, PromotionDTO? Campaign, string? Message)> CreateFeaturedAsync(CreateFeaturedPromotionDTO dto, string ownerId);
        Task<(OwnershipResult Result, PromotionDTO? Campaign, string? Message)> CreateNotificationAsync(CreateNotificationPromotionDTO dto, string ownerId);
        Task<PromotionDTO?> ApproveCampaignAsync(int id, string? reason = null);
        Task<bool> RejectCampaignAsync(int id, string? reason = null);
        Task<IReadOnlyList<PromotionDTO>> GetSponsoredProductsAsync(int limit = 3);
        Task<IReadOnlyList<PromotionDTO>> GetSponsoredStoresAsync(int limit = 3);
        Task<bool> TrackPromotionClickAsync(int id);
        Task RecordSearchAsync(string query);
        Task<IReadOnlyList<TrendingQueryDTO>> GetTrendingAsync(int limit = 8);
        Task<BillingSummaryDTO?> GetBillingAsync(string ownerId);
        Task<BillingSummaryDTO?> TopUpAsync(string ownerId, TopUpWalletDTO dto);
        Task<MerchantLeadDTO?> CreateLeadAsync(CreateLeadDTO dto, string? userId);
        Task<IReadOnlyList<MerchantLeadDTO>> GetMerchantLeadsAsync(string ownerId);
        Task<AlertDTO?> CreateAlertAsync(CreateAlertDTO dto, string userId);
        Task<IReadOnlyList<AlertDTO>> GetAlertsAsync(string userId);
        Task<NotificationPreferencesDTO?> GetNotificationPreferencesAsync(string userId);
        Task<NotificationPreferencesDTO?> SetNotificationPreferencesAsync(string userId, NotificationPreferencesDTO dto);
        Task<OwnershipResult> ScheduleSaleAsync(int productId, ScheduleSaleDTO dto, string userId, bool isAdmin);
        Task<MerchantInsightsDTO?> GetInsightsAsync(string ownerId);
        Task<AiProductSuggestionDTO> SuggestProductFieldsAsync(AiProductSuggestionRequestDTO dto);
        AiProductResultDto? ToSponsoredAiProduct(PromotionDTO campaign);
    }
}
