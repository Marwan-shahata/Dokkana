using System.Globalization;
using System.Text.Json;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.Monetization;
using Dokkana.BLL.Services;
using Dokkana.Common;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class MonetizationManager : IMonetizationManager
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationManager _notifications;
        private readonly IOpenRouterClient _openRouter;
        private readonly IFileService _files;

        public MonetizationManager(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationManager notifications,
            IOpenRouterClient openRouter,
            IFileService files)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
            _openRouter = openRouter;
            _files = files;
        }

        public async Task<IReadOnlyList<PromotionDTO>> GetMerchantCampaignsAsync(string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return [];

            var campaigns = await CampaignQuery()
                .Where(p => p.StoreId == store.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return campaigns.Select(MapPromotion).ToList();
        }

        public async Task<IReadOnlyList<PromotionDTO>> GetAdminCampaignsAsync(string? status = null)
        {
            var query = CampaignQuery();
            if (Enum.TryParse<PromotionStatus>(status, ignoreCase: true, out var parsed))
                query = query.Where(p => p.Status == parsed);

            var campaigns = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return campaigns.Select(MapPromotion).ToList();
        }

        public async Task<(OwnershipResult Result, PromotionDTO? Campaign, string? Message)> CreateFeaturedAsync(
            CreateFeaturedPromotionDTO dto,
            string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return (OwnershipResult.Forbidden, null, "Create a store before promoting.");

            Product? product = null;
            if (dto.ProductId.HasValue)
            {
                product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId.Value);
                if (product is null) return (OwnershipResult.NotFound, null, "Product not found.");
                if (product.StoreId != store.Id) return (OwnershipResult.Forbidden, null, "You can promote only your own products.");
            }

            var now = DateTime.UtcNow;
            var campaign = new Promotion
            {
                StoreId = store.Id,
                ProductId = product?.Id,
                Type = product is null ? PromotionType.FeaturedStore : PromotionType.FeaturedProduct,
                Budget = Math.Max(dto.Budget, 0),
                Status = PromotionStatus.Pending,
                Audience = PromotionAudience.AllShoppers,
                StartAt = now,
                EndAt = now.AddDays(Math.Clamp(dto.DurationDays, 1, 30))
            };

            _context.Promotions.Add(campaign);
            await _context.SaveChangesAsync();

            return (OwnershipResult.Success, await GetCampaign(campaign.Id), "Campaign submitted for admin approval.");
        }

        public async Task<(OwnershipResult Result, PromotionDTO? Campaign, string? Message)> CreateNotificationAsync(
            CreateNotificationPromotionDTO dto,
            string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return (OwnershipResult.Forbidden, null, "Create a store before promoting.");

            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
                return (OwnershipResult.Forbidden, null, "Title and message are required.");

            Enum.TryParse<PromotionAudience>(dto.Audience, ignoreCase: true, out var audience);
            var now = DateTime.UtcNow;
            var campaign = new Promotion
            {
                StoreId = store.Id,
                Type = PromotionType.SponsoredNotification,
                Title = dto.Title.Trim(),
                Message = dto.Message.Trim(),
                Audience = audience,
                Budget = Math.Max(dto.Budget, 0),
                Status = PromotionStatus.Pending,
                StartAt = now,
                EndAt = now.AddDays(Math.Clamp(dto.DurationDays, 1, 14))
            };

            _context.Promotions.Add(campaign);
            await _context.SaveChangesAsync();

            return (OwnershipResult.Success, await GetCampaign(campaign.Id), "Sponsored notification submitted for approval.");
        }

        public async Task<PromotionDTO?> ApproveCampaignAsync(int id, string? reason = null)
        {
            var campaign = await _context.Promotions
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (campaign is null) return null;

            campaign.Status = PromotionStatus.Approved;
            campaign.ApprovedAt = DateTime.UtcNow;
            campaign.RejectionReason = null;

            if (campaign.Store is not null && campaign.Budget > 0)
            {
                campaign.Store.WalletBalance -= campaign.Budget;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    StoreId = campaign.StoreId,
                    Amount = -campaign.Budget,
                    Type = "CampaignSpend",
                    Note = $"Campaign #{campaign.Id} approved"
                });
            }

            if (campaign.Type == PromotionType.SponsoredNotification)
                campaign.RecipientsCount = await SendSponsoredNotificationsAsync(campaign);

            await _context.SaveChangesAsync();
            return await GetCampaign(campaign.Id);
        }

        public async Task<bool> RejectCampaignAsync(int id, string? reason = null)
        {
            var campaign = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
            if (campaign is null) return false;

            campaign.Status = PromotionStatus.Rejected;
            campaign.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by admin." : reason.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<PromotionDTO>> GetSponsoredProductsAsync(int limit = 3)
        {
            var now = DateTime.UtcNow;
            var campaigns = await CampaignQuery()
                .Where(p => p.Type == PromotionType.FeaturedProduct &&
                            p.ProductId != null &&
                            (p.Status == PromotionStatus.Approved || p.Status == PromotionStatus.Active) &&
                            (p.StartAt == null || p.StartAt <= now) &&
                            (p.EndAt == null || p.EndAt >= now))
                .OrderByDescending(p => p.Budget)
                .ThenByDescending(p => p.CreatedAt)
                .Take(Math.Clamp(limit, 1, 12))
                .ToListAsync();

            foreach (var campaign in campaigns)
                campaign.Impressions += 1;

            await _context.SaveChangesAsync();
            return campaigns.Select(MapPromotion).ToList();
        }

        public async Task<IReadOnlyList<PromotionDTO>> GetSponsoredStoresAsync(int limit = 3)
        {
            var now = DateTime.UtcNow;
            var campaigns = await CampaignQuery()
                .Where(p => (p.Type == PromotionType.FeaturedStore || p.Type == PromotionType.FeaturedProduct) &&
                            (p.Status == PromotionStatus.Approved || p.Status == PromotionStatus.Active) &&
                            (p.StartAt == null || p.StartAt <= now) &&
                            (p.EndAt == null || p.EndAt >= now))
                .GroupBy(p => p.StoreId)
                .Select(g => g.OrderByDescending(p => p.Budget).First())
                .OrderByDescending(p => p.Budget)
                .Take(Math.Clamp(limit, 1, 12))
                .ToListAsync();

            foreach (var campaign in campaigns)
                campaign.Impressions += 1;

            await _context.SaveChangesAsync();
            return campaigns.Select(MapPromotion).ToList();
        }

        public async Task<bool> TrackPromotionClickAsync(int id)
        {
            var campaign = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
            if (campaign is null) return false;
            campaign.Clicks += 1;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task RecordSearchAsync(string query)
        {
            var normalized = NormalizeQuery(query);
            if (string.IsNullOrWhiteSpace(normalized)) return;

            var trend = await _context.SearchTrends.FirstOrDefaultAsync(t => t.NormalizedQuery == normalized);
            if (trend is null)
            {
                _context.SearchTrends.Add(new SearchTrend
                {
                    Query = query.Trim(),
                    NormalizedQuery = normalized,
                    Count = 1
                });
            }
            else
            {
                trend.Count += 1;
                trend.Query = query.Trim();
                trend.LastSearchedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TrendingQueryDTO>> GetTrendingAsync(int limit = 8)
        {
            return await _context.SearchTrends
                .AsNoTracking()
                .OrderByDescending(t => t.Count)
                .ThenByDescending(t => t.LastSearchedAt)
                .Take(Math.Clamp(limit, 1, 20))
                .Select(t => new TrendingQueryDTO { Query = t.Query, Count = t.Count })
                .ToListAsync();
        }

        public async Task<BillingSummaryDTO?> GetBillingAsync(string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return null;

            return new BillingSummaryDTO
            {
                Balance = store.WalletBalance,
                Transactions = await _context.WalletTransactions
                    .AsNoTracking()
                    .Where(t => t.StoreId == store.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(30)
                    .Select(t => new WalletTransactionDTO
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Type = t.Type,
                        Note = t.Note,
                        CreatedAt = t.CreatedAt
                    })
                    .ToListAsync()
            };
        }

        public async Task<BillingSummaryDTO?> TopUpAsync(string ownerId, TopUpWalletDTO dto)
        {
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
            if (store is null) return null;

            var amount = Math.Max(dto.Amount, 0);
            store.WalletBalance += amount;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                StoreId = store.Id,
                Amount = amount,
                Type = "TopUp",
                Note = string.IsNullOrWhiteSpace(dto.Note) ? "Demo wallet top-up" : dto.Note.Trim()
            });
            await _context.SaveChangesAsync();
            return await GetBillingAsync(ownerId);
        }

        public async Task<MerchantLeadDTO?> CreateLeadAsync(CreateLeadDTO dto, string? userId)
        {
            var store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.StoreId);
            if (store is null) return null;

            var user = string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
            var lead = new MerchantLead
            {
                StoreId = dto.StoreId,
                ProductId = dto.ProductId,
                UserId = user?.Id,
                Type = string.IsNullOrWhiteSpace(dto.Type) ? "WhatsApp" : dto.Type.Trim(),
                CustomerName = user is null ? null : $"{user.FirstName} {user.LastName}".Trim(),
                CustomerEmail = user?.Email
            };

            _context.MerchantLeads.Add(lead);
            await _context.SaveChangesAsync();
            var leadRow = await _context.MerchantLeads
                .AsNoTracking()
                .Include(l => l.Store)
                .Include(l => l.Product)
                .Where(l => l.Id == lead.Id)
                .FirstOrDefaultAsync();
            return leadRow is null ? null : MapLead(leadRow);
        }

        public async Task<IReadOnlyList<MerchantLeadDTO>> GetMerchantLeadsAsync(string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return [];

            var leads = await _context.MerchantLeads
                .AsNoTracking()
                .Include(l => l.Store)
                .Include(l => l.Product)
                .Where(l => l.StoreId == store.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .ToListAsync();
            return leads.Select(MapLead).ToList();
        }

        public async Task<AlertDTO?> CreateAlertAsync(CreateAlertDTO dto, string userId)
        {
            if (!Enum.TryParse<ShopperAlertType>(dto.Type, ignoreCase: true, out var type))
                type = dto.ProductId.HasValue ? ShopperAlertType.BackInStock : ShopperAlertType.NewMatch;

            if (!dto.ProductId.HasValue && string.IsNullOrWhiteSpace(dto.Query))
                return null;

            var alert = new ShopperAlert
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Type = type,
                Query = string.IsNullOrWhiteSpace(dto.Query) ? null : dto.Query.Trim(),
                TargetPrice = dto.TargetPrice
            };

            _context.ShopperAlerts.Add(alert);
            await _context.SaveChangesAsync();
            var alertRow = await _context.ShopperAlerts
                .AsNoTracking()
                .Include(a => a.Product)
                .Where(a => a.Id == alert.Id)
                .FirstOrDefaultAsync();
            return alertRow is null ? null : MapAlert(alertRow);
        }

        public async Task<IReadOnlyList<AlertDTO>> GetAlertsAsync(string userId)
        {
            var alerts = await _context.ShopperAlerts
                .AsNoTracking()
                .Include(a => a.Product)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return alerts.Select(MapAlert).ToList();
        }

        public async Task<NotificationPreferencesDTO?> GetNotificationPreferencesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user is null
                ? null
                : new NotificationPreferencesDTO { SponsoredNotificationsEnabled = user.SponsoredNotificationsEnabled };
        }

        public async Task<NotificationPreferencesDTO?> SetNotificationPreferencesAsync(
            string userId,
            NotificationPreferencesDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;

            user.SponsoredNotificationsEnabled = dto.SponsoredNotificationsEnabled;
            await _userManager.UpdateAsync(user);
            return new NotificationPreferencesDTO { SponsoredNotificationsEnabled = user.SponsoredNotificationsEnabled };
        }

        public async Task<OwnershipResult> ScheduleSaleAsync(int productId, ScheduleSaleDTO dto, string userId, bool isAdmin)
        {
            var product = await _context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product is null) return OwnershipResult.NotFound;
            if (!isAdmin && product.Store?.OwnerId != userId) return OwnershipResult.Forbidden;
            if (dto.SalePrice <= 0 || dto.SalePrice >= product.Price) return OwnershipResult.Forbidden;

            product.OriginalPrice ??= product.Price;
            product.SalePrice = dto.SalePrice;
            product.SaleStartsAt = dto.StartsAt ?? DateTime.UtcNow;
            product.SaleEndsAt = dto.EndsAt ?? DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            if (product.SaleStartsAt <= DateTime.UtcNow)
                await NotifyFollowersAboutSale(product);

            return OwnershipResult.Success;
        }

        public async Task<AiProductSuggestionDTO> SuggestProductFieldsAsync(AiProductSuggestionRequestDTO dto)
        {
            // Real image understanding when an OpenRouter key + a usable image exist;
            // otherwise fall back to the keyword heuristic so the demo still works offline.
            if (!_openRouter.IsConfigured) return HeuristicSuggest(dto);

            var imageRef = await _files.TryReadImageAsDataUrlAsync(dto.ImageUrl);
            if (imageRef is null &&
                (dto.ImageUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                imageRef = dto.ImageUrl; // a public URL the model can fetch directly
            }
            if (imageRef is null) return HeuristicSuggest(dto);

            try
            {
                const string system =
                    "You are a fashion catalog assistant for an Egyptian marketplace. Look at the product " +
                    "image and reply with ONLY a compact JSON object (no markdown, no prose) with keys: " +
                    "title, description, color, size, brand, gender, categoryName. categoryName must be exactly " +
                    "one of: Dresses, Tops, Pants, Shoes, Bags, Accessories, Outerwear, Activewear. gender must " +
                    "be one of: Women, Men, Unisex. description is one short shopper-friendly line (max 200 " +
                    "chars). If the brand is not visible, use \"Local\".";

                var raw = await _openRouter.AskWithImageAsync(system, "Describe this product as JSON.", imageRef);
                return ParseSuggestion(raw) ?? HeuristicSuggest(dto);
            }
            catch
            {
                // Network / model / parse failure → graceful fallback.
                return HeuristicSuggest(dto);
            }
        }

        // Extracts the suggestion JSON the vision model returns (tolerating code
        // fences or surrounding prose). Returns null when nothing usable is found.
        private static AiProductSuggestionDTO? ParseSuggestion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return null;

            try
            {
                using var doc = JsonDocument.Parse(raw.Substring(start, end - start + 1));
                var root = doc.RootElement;
                string Get(string key) =>
                    root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
                        ? (v.GetString() ?? string.Empty).Trim()
                        : string.Empty;

                var title = Get("title");
                if (string.IsNullOrWhiteSpace(title)) return null;

                var brand = Get("brand");
                return new AiProductSuggestionDTO
                {
                    Title = title,
                    Description = Get("description"),
                    Color = Get("color"),
                    Size = Get("size"),
                    Brand = string.IsNullOrWhiteSpace(brand) ? "Local" : brand,
                    Gender = Get("gender"),
                    CategoryName = Get("categoryName")
                };
            }
            catch
            {
                return null;
            }
        }

        // Offline fallback: derives fields from the file name / URL keywords.
        private AiProductSuggestionDTO HeuristicSuggest(AiProductSuggestionRequestDTO dto)
        {
            var source = $"{dto.FileName} {dto.ImageUrl}".ToLowerInvariant();
            var category = source.Contains("shoe") || source.Contains("sneaker") || source.Contains("كوتش")
                ? "Shoes"
                : source.Contains("bag") || source.Contains("شنط")
                    ? "Bags"
                    : source.Contains("pant") || source.Contains("jean")
                        ? "Pants"
                        : source.Contains("jacket") || source.Contains("coat")
                            ? "Outerwear"
                            : source.Contains("dress") || source.Contains("فستان")
                                ? "Dresses"
                                : "Tops";

            var color = FirstColor(source);
            var title = $"{color} {category.TrimEnd('s')}".Trim();
            return new AiProductSuggestionDTO
            {
                Title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title.ToLowerInvariant()),
                Description = $"AI draft: {color.ToLowerInvariant()} {category.ToLowerInvariant()} item, ready for Arabic/English shopper search.",
                Color = color,
                Size = category == "Shoes" ? "42" : "M",
                Brand = "Local",
                Gender = category is "Dresses" ? "Women" : "Unisex",
                CategoryName = category
            };
        }

        public AiProductResultDto? ToSponsoredAiProduct(PromotionDTO campaign)
        {
            if (!campaign.ProductId.HasValue || campaign.ProductPrice is null) return null;
            return new AiProductResultDto
            {
                Id = campaign.ProductId.Value,
                Title = campaign.ProductTitle ?? campaign.Title ?? "Sponsored product",
                Price = campaign.ProductPrice.Value,
                OriginalPrice = campaign.ProductOriginalPrice,
                SalePrice = campaign.IsProductOnSale ? campaign.ProductPrice.Value : null,
                IsOnSale = campaign.IsProductOnSale,
                StoreId = campaign.StoreId,
                StoreName = campaign.StoreName,
                ImageUrl = campaign.ProductImageUrl,
                Score = 999,
                Reason = "Sponsored placement from a verified local store",
                IsSponsored = true,
                PromotionId = campaign.Id
            };
        }

        // MM3 (Pro-only): demand forecast + pricing hints for the owner's store.
        // Free stores get IsPro=false and empty lists (the UI shows an upsell).
        public async Task<MerchantInsightsDTO?> GetInsightsAsync(string ownerId)
        {
            var store = await StoreForOwner(ownerId);
            if (store is null) return null;

            var result = new MerchantInsightsDTO { IsPro = store.Tier == StoreTier.Pro };
            if (!result.IsPro) return result;

            var since = DateTime.UtcNow.AddDays(-30);

            var products = await _context.Products.AsNoTracking()
                .Where(p => p.StoreId == store.Id)
                .Select(p => new { p.Id, p.Title, p.Count, p.Price, p.CategoryId })
                .ToListAsync();

            // Units sold per product over the last 30 days (completed orders only).
            var sales = await _context.OrderItems.AsNoTracking()
                .Where(oi => oi.StoreId == store.Id
                          && oi.ProductId != null
                          && oi.Order!.Status == OrderStatus.Completed
                          && oi.Order.CreatedAt >= since)
                .GroupBy(oi => oi.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Units = g.Sum(x => x.Quantity) })
                .ToListAsync();
            var sold = sales.ToDictionary(s => s.ProductId, s => s.Units);

            // Demand forecast: estimated days until stock runs out at the recent rate.
            foreach (var p in products)
            {
                if (p.Count <= 0) continue;
                sold.TryGetValue(p.Id, out var units);
                if (units <= 0) continue; // no recent sales → no forecast
                var dailyRate = units / 30.0;
                var days = (int)Math.Ceiling(p.Count / dailyRate);
                result.Forecasts.Add(new DemandForecastDTO
                {
                    ProductId = p.Id,
                    ProductTitle = p.Title,
                    Stock = p.Count,
                    UnitsSold30d = units,
                    DaysToSellout = days,
                    Severity = days <= 7 ? "urgent" : days <= 21 ? "soon" : "ok"
                });
            }
            result.Forecasts = result.Forecasts.OrderBy(f => f.DaysToSellout).Take(5).ToList();

            // Pricing hints: compare each product to the same category in OTHER stores.
            var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
            var market = await _context.Products.AsNoTracking()
                .Where(p => categoryIds.Contains(p.CategoryId) && p.StoreId != store.Id)
                .Select(p => new { p.CategoryId, p.Price })
                .ToListAsync();
            var byCategory = market
                .GroupBy(m => m.CategoryId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Price).OrderBy(x => x).ToList());

            foreach (var p in products)
            {
                if (!byCategory.TryGetValue(p.CategoryId, out var prices) || prices.Count == 0) continue;
                var median = prices[prices.Count / 2];
                var verdict = p.Price > median * 1.15m ? "high"
                            : p.Price < median * 0.85m ? "low" : "fair";
                if (verdict == "fair") continue;
                result.PricingHints.Add(new PricingHintDTO
                {
                    ProductId = p.Id,
                    ProductTitle = p.Title,
                    Price = p.Price,
                    MarketLow = prices[0],
                    MarketHigh = prices[^1],
                    MarketMedian = median,
                    Verdict = verdict
                });
            }
            result.PricingHints = result.PricingHints.Take(5).ToList();

            return result;
        }

        private async Task<int> SendSponsoredNotificationsAsync(Promotion campaign)
        {
            var recipients = await CampaignRecipients(campaign);
            var sent = 0;
            foreach (var user in recipients)
            {
                if (!user.SponsoredNotificationsEnabled) continue;
                var sentToday = await _context.Notifications.CountAsync(n =>
                    n.UserId == user.Id &&
                    n.IsSponsored &&
                    n.CreatedAt >= DateTime.UtcNow.AddDays(-1));
                if (sentToday >= 2) continue;

                await _notifications.NotifyUserAsync(
                    user.Id,
                    campaign.Title ?? "Sponsored from Dokkana",
                    campaign.Message ?? "A local store has a new offer for you.",
                    "Sponsored",
                    $"/stores/{campaign.StoreId}",
                    true,
                    campaign.Id);
                sent++;
            }

            return sent;
        }

        private async Task<IReadOnlyList<ApplicationUser>> CampaignRecipients(Promotion campaign)
        {
            if (campaign.Audience == PromotionAudience.Followers)
            {
                var followerIds = await _context.StoreFollows
                    .Where(f => f.StoreId == campaign.StoreId)
                    .Select(f => f.UserId)
                    .ToListAsync();
                return await _context.Users.Where(u => followerIds.Contains(u.Id)).ToListAsync();
            }

            if (campaign.Audience == PromotionAudience.WishlistMatches && campaign.ProductId.HasValue)
            {
                var ids = await _context.FavoriteProducts
                    .Where(f => f.ProductId == campaign.ProductId.Value)
                    .Select(f => f.UserId)
                    .ToListAsync();
                return await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            }

            return (await _userManager.GetUsersInRoleAsync("Shopper")).ToList();
        }

        private async Task NotifyFollowersAboutSale(Product product)
        {
            var followerIds = await _context.StoreFollows
                .Where(f => f.StoreId == product.StoreId)
                .Select(f => f.UserId)
                .ToListAsync();

            foreach (var userId in followerIds)
            {
                await _notifications.NotifyUserAsync(
                    userId,
                    "Sale started",
                    $"{product.Title} is now on sale.",
                    "Sale",
                    $"/products/{product.Id}");
            }
        }

        private async Task<Store?> StoreForOwner(string ownerId)
        {
            return await _context.Stores.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
        }

        private async Task<PromotionDTO?> GetCampaign(int id)
        {
            var campaign = await CampaignQuery()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
            return campaign is null ? null : MapPromotion(campaign);
        }

        private IQueryable<Promotion> CampaignQuery()
        {
            return _context.Promotions
                .Include(p => p.Store)
                .Include(p => p.Product);
        }

        private static PromotionDTO MapPromotion(Promotion p)
        {
            var activeSale = p.Product?.SalePrice != null &&
                             (p.Product.SaleStartsAt == null || p.Product.SaleStartsAt <= DateTime.UtcNow) &&
                             (p.Product.SaleEndsAt == null || p.Product.SaleEndsAt >= DateTime.UtcNow);

            return new PromotionDTO
            {
                Id = p.Id,
                StoreId = p.StoreId,
                StoreName = p.Store?.Name ?? string.Empty,
                Type = p.Type.ToString(),
                Status = p.Status.ToString(),
                ProductId = p.ProductId,
                ProductTitle = p.Product?.Title,
                ProductPrice = activeSale ? p.Product?.SalePrice : p.Product?.Price,
                ProductOriginalPrice = activeSale ? p.Product?.OriginalPrice ?? p.Product?.Price : null,
                IsProductOnSale = activeSale,
                ProductImageUrl = p.Product?.ImageUrl,
                Title = p.Title,
                Message = p.Message,
                Audience = p.Audience.ToString(),
                Budget = p.Budget,
                Impressions = p.Impressions,
                Clicks = p.Clicks,
                RecipientsCount = p.RecipientsCount,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt,
                ApprovedAt = p.ApprovedAt,
                StartAt = p.StartAt,
                EndAt = p.EndAt
            };
        }

        private static MerchantLeadDTO MapLead(MerchantLead l) => new()
        {
            Id = l.Id,
            StoreId = l.StoreId,
            StoreName = l.Store?.Name ?? string.Empty,
            ProductId = l.ProductId,
            ProductTitle = l.Product?.Title,
            Type = l.Type,
            CustomerName = l.CustomerName,
            CustomerEmail = l.CustomerEmail,
            CreatedAt = l.CreatedAt
        };

        private static AlertDTO MapAlert(ShopperAlert a) => new()
        {
            Id = a.Id,
            Type = a.Type.ToString(),
            ProductId = a.ProductId,
            ProductTitle = a.Product?.Title,
            Query = a.Query,
            TargetPrice = a.TargetPrice,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            LastTriggeredAt = a.LastTriggeredAt
        };

        private static string NormalizeQuery(string query)
        {
            return string.Join(" ", query.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string FirstColor(string source)
        {
            foreach (var color in new[] { "Black", "White", "Red", "Blue", "Green", "Yellow", "Brown", "Beige", "Pink", "Gray" })
            {
                if (source.Contains(color.ToLowerInvariant())) return color;
            }

            return "Black";
        }
    }
}
