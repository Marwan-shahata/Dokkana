using Dokkana.BLL.AI.Agents;
using Dokkana.BLL.AI.Search;
using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiSearchController : ControllerBase
    {
        private const int MaxQueryLength = 300;
        private const long MaxImageBytes = 8_000_000;

        private readonly IAiSearchManager _aiSearchManager;
        private readonly IProductSemanticSearchService _semanticSearch;
        private readonly IMonetizationManager _monetization;

        public AiSearchController(
            IAiSearchManager aiSearchManager,
            IProductSemanticSearchService semanticSearch,
            IMonetizationManager monetization)
        {
            _aiSearchManager = aiSearchManager;
            _semanticSearch = semanticSearch;
            _monetization = monetization;
        }

        // Natural-language product search (Arabic / English / Franco-Arabic).
        // The manager interprets the query, searches the local catalog, optionally
        // adds web results, then ranks everything. Wrapped in GeneralResult so the
        // response matches the rest of the API (frontend expects { success, data }).
        [HttpPost("search")]
        [EnableRateLimiting("ai")]
        public async Task<IActionResult> Search(
            [FromBody] AiSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(GeneralResult.Fail("Query is required."));

            // Basic prompt guardrail: cap length and strip control characters
            // before the text reaches the LLM or the search agents.
            request.Query = Sanitize(request.Query);

            var result = await _aiSearchManager.SearchAsync(request, cancellationToken);
            await _monetization.RecordSearchAsync(request.Query);
            await AddSponsoredSlotAsync(result);

            return Ok(GeneralResult<AiSearchResponseDto>.Ok(result));
        }

        // Visual search: upload a photo, get the most visually-similar products from
        // the catalog (the embedding model is multimodal, so the image is matched
        // against the same product vectors text search uses).
        [HttpPost("search/image")]
        [EnableRateLimiting("ai")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<IActionResult> ImageSearch(
            [FromForm] IFormFile image,
            [FromForm] double? userLat,
            [FromForm] double? userLng,
            CancellationToken cancellationToken)
        {
            if (image is null || image.Length == 0)
                return BadRequest(GeneralResult.Fail("Image is required."));
            if (image.Length > MaxImageBytes)
                return BadRequest(GeneralResult.Fail("Image is too large (max 8MB)."));

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms, cancellationToken);

            var request = new AiSearchRequestDto
            {
                Query = "[image]",
                IncludeWeb = false,
                PageSize = 12,
                UserLat = userLat,
                UserLng = userLng
            };

            var matches = await _semanticSearch.SearchByImageAsync(
                ms.ToArray(), image.ContentType ?? "image/jpeg", request, 12, cancellationToken);

            var response = new AiSearchResponseDto
            {
                Query = "[image search]",
                LocalResults = matches,
                RankedResults = matches.Select(p => new AiRankedResultDto
                {
                    Source = "local",
                    Title = p.Title,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    StoreName = p.StoreName,
                    Score = p.Score,
                    Reason = p.Reason,
                    Product = p
                }).ToList(),
                Answer = matches.Any()
                    ? $"لقيت {matches.Count} منتجات قريبة من الصورة المرفقة."
                    : "مش لاقي منتجات قريبة من الصورة. جرّب صورة أوضح بخلفية بسيطة."
            };

            return Ok(GeneralResult<AiSearchResponseDto>.Ok(response));
        }

        private static string Sanitize(string query)
        {
            var trimmed = query.Trim();
            if (trimmed.Length > MaxQueryLength)
                trimmed = trimmed[..MaxQueryLength];
            // Drop control chars (defensive against prompt-injection via newlines/escapes).
            return new string(trimmed.Where(c => !char.IsControl(c)).ToArray());
        }

        private async Task AddSponsoredSlotAsync(AiSearchResponseDto result)
        {
            var campaign = (await _monetization.GetSponsoredProductsAsync(1)).FirstOrDefault();
            if (campaign is null) return;

            var product = _monetization.ToSponsoredAiProduct(campaign);
            if (product is null) return;

            result.LocalResults = new[] { product }
                .Concat(result.LocalResults.Where(p => p.Id != product.Id))
                .ToList();
            result.RankedResults = new[]
            {
                new AiRankedResultDto
                {
                    Source = "local",
                    Title = product.Title,
                    Price = product.Price,
                    OriginalPrice = product.OriginalPrice,
                    SalePrice = product.SalePrice,
                    IsOnSale = product.IsOnSale,
                    ImageUrl = product.ImageUrl,
                    StoreName = product.StoreName,
                    Score = 999,
                    Reason = product.Reason,
                    Product = product,
                    IsSponsored = true,
                    PromotionId = campaign.Id
                }
            }
            .Concat(result.RankedResults.Where(r => r.Product?.Id != product.Id))
            .ToList();
        }
    }
}
