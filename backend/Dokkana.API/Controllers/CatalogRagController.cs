using System.Security.Claims;
using Dokkana.BLL.AI.Catalog;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers;

// Catalog RAG: upload a PDF catalog, then ask questions over it. Embeddings are
// stored as JSON in SQL Server and retrieved with hybrid (vector + keyword)
// scoring. See AI features guide section 7.
[ApiController]
[Route("api/ai/catalog")]
[Authorize(Policy = "StoreOwnerOnly")]
public class CatalogRagController : ControllerBase
{
    private readonly ICatalogRagManager _catalogRagManager;
    private readonly ICatalogEmbeddingMaintenanceService _embeddingMaintenanceService;
    private readonly IStoreManager _storeManager;

    public CatalogRagController(
        ICatalogRagManager catalogRagManager,
        ICatalogEmbeddingMaintenanceService embeddingMaintenanceService,
        IStoreManager storeManager)
    {
        _catalogRagManager = catalogRagManager;
        _embeddingMaintenanceService = embeddingMaintenanceService;
        _storeManager = storeManager;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] int storeId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(GeneralResult<CatalogUploadResponseDto>.Fail("ارفع ملف PDF صحيح."));
        }

        var ownedStoreId = await GetOwnedStoreIdAsync();
        if (!ownedStoreId.HasValue)
            return BadRequest(GeneralResult.Fail("Create your store profile first."));

        if (storeId > 0 && storeId != ownedStoreId.Value)
            return Forbid();

        await using var stream = file.OpenReadStream();

        var result = await _catalogRagManager.UploadCatalogAsync(
            ownedStoreId.Value,
            stream,
            file.FileName,
            cancellationToken);

        if (result.Status == "Failed")
            return BadRequest(GeneralResult<CatalogUploadResponseDto>.Ok(result, result.Message));

        return Ok(GeneralResult<CatalogUploadResponseDto>.Ok(result));
    }

    [HttpPost("ask")]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Ask(
        [FromBody] CatalogQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownedStoreId = await GetOwnedStoreIdAsync();
        if (!ownedStoreId.HasValue)
            return BadRequest(GeneralResult.Fail("Create your store profile first."));

        if (request.StoreId > 0 && request.StoreId != ownedStoreId.Value)
            return Forbid();

        request.StoreId = ownedStoreId.Value;

        var result = await _catalogRagManager.AskAsync(request, cancellationToken);
        return Ok(GeneralResult<CatalogQuestionResponseDto>.Ok(result));
    }

    [HttpPost("rebuild-embeddings")]
    public async Task<IActionResult> RebuildEmbeddings(
        [FromBody] RebuildCatalogEmbeddingsRequestDto? request,
        CancellationToken cancellationToken)
    {
        request ??= new RebuildCatalogEmbeddingsRequestDto();

        var ownedStoreId = await GetOwnedStoreIdAsync();
        if (!ownedStoreId.HasValue)
            return BadRequest(GeneralResult.Fail("Create your store profile first."));

        if (request.StoreId.HasValue && request.StoreId.Value != ownedStoreId.Value)
            return Forbid();

        request.StoreId = ownedStoreId.Value;

        var result = await _embeddingMaintenanceService.RebuildAsync(request, cancellationToken);
        return Ok(GeneralResult<RebuildCatalogEmbeddingsResponseDto>.Ok(result));
    }

    private async Task<int?> GetOwnedStoreIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var ownedStore = await _storeManager.GetByOwnerAsync(userId);
        return ownedStore?.Id;
    }
}
