using System.Security.Claims;
using Dokkana.BLL.AI.Copilot;
using Dokkana.BLL.DTOs.AI.Copilot;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers;

// Store-owner analytics Q&A (top sellers, sales summary, low stock, unsold items)
// answered from real completed orders. See AI features guide section 6.
[ApiController]
[Route("api/ai/copilot")]
[Authorize(Policy = "StoreOwnerOnly")]
public class BusinessCopilotController : ControllerBase
{
    private readonly IBusinessCopilotManager _businessCopilotManager;
    private readonly IStoreManager _storeManager;

    public BusinessCopilotController(
        IBusinessCopilotManager businessCopilotManager,
        IStoreManager storeManager)
    {
        _businessCopilotManager = businessCopilotManager;
        _storeManager = storeManager;
    }

    [HttpPost("ask")]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Ask(
        [FromBody] BusinessCopilotRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(GeneralResult.Fail("Missing user."));

        var ownedStore = await _storeManager.GetByOwnerAsync(userId);
        if (ownedStore is null)
            return BadRequest(GeneralResult.Fail("Create your store profile first."));

        if (request.StoreId > 0 && request.StoreId != ownedStore.Id)
            return Forbid();

        request.StoreId = ownedStore.Id;

        var result = await _businessCopilotManager.AskAsync(request, cancellationToken);
        return Ok(GeneralResult<BusinessCopilotResponseDto>.Ok(result));
    }
}
