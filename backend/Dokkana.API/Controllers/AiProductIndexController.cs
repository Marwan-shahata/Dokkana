using Dokkana.BLL.AI.Search;
using Dokkana.BLL.DTOs.AI;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers;

// Builds / inspects the semantic (vector) index over the product catalog — the
// "RAG over our database" feature. Call reindex once after configuring an
// OpenRouter key (and again whenever products change a lot).
[ApiController]
[Route("api/ai/products")]
[Authorize(Policy = "AdminOnly")]
public class AiProductIndexController : ControllerBase
{
    private readonly IProductSemanticSearchService _semanticSearch;

    public AiProductIndexController(IProductSemanticSearchService semanticSearch)
    {
        _semanticSearch = semanticSearch;
    }

    // GET /api/ai/products/index-status — is semantic search on, and how many
    // products are embedded?
    [HttpGet("index-status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var indexed = await _semanticSearch.CountIndexedAsync(cancellationToken);
        return Ok(GeneralResult<object>.Ok(new
        {
            configured = _semanticSearch.IsConfigured,
            indexedProducts = indexed,
            mode = _semanticSearch.IsConfigured
                ? (indexed > 0 ? "semantic+keyword" : "keyword (run reindex to enable semantic)")
                : "keyword (no OpenRouter key)"
        }));
    }

    // POST /api/ai/products/reindex — embed the catalog. Body: { "forceRebuild": true|false }
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(
        [FromBody] ProductReindexRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _semanticSearch.ReindexAsync(
            request?.ForceRebuild ?? false,
            cancellationToken);

        return Ok(GeneralResult<ProductReindexResultDto>.Ok(result, result.Message));
    }

    public class ProductReindexRequest
    {
        public bool ForceRebuild { get; set; }
    }
}
