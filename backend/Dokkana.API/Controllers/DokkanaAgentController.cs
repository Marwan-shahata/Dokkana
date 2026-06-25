using System.Security.Claims;
using Dokkana.BLL.AI.Orchestrator;
using Dokkana.BLL.DTOs.AI.Agent;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers;

// Unified AI entry point: routes one natural-language message to product search,
// catalog RAG, or business copilot. This is the endpoint the frontend/demo uses.
[ApiController]
[Route("api/ai")]
public class DokkanaAgentController : ControllerBase
{
    private readonly IDokkanaAgentManager _dokkanaAgentManager;
    private readonly IStoreManager _storeManager;

    public DokkanaAgentController(
        IDokkanaAgentManager dokkanaAgentManager,
        IStoreManager storeManager)
    {
        _dokkanaAgentManager = dokkanaAgentManager;
        _storeManager = storeManager;
    }

    [HttpPost("ask")]
    [Authorize]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Ask(
        [FromBody] DokkanaAgentRequestDto request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await ApplyStoreScopeAsync(request);
        if (scopeResult is not null)
            return scopeResult;

        var result = await _dokkanaAgentManager.AskAsync(request, cancellationToken);
        return Ok(GeneralResult<DokkanaAgentResponseDto>.Ok(result));
    }

    private async Task<IActionResult?> ApplyStoreScopeAsync(DokkanaAgentRequestDto request)
    {
        if (User.IsInRole("Admin"))
        {
            request.UserRole = "admin";
            return null;
        }

        if (User.IsInRole("StoreOwner"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(GeneralResult.Fail("Missing user."));

            var ownedStore = await _storeManager.GetByOwnerAsync(userId);
            if (ownedStore is null)
                return BadRequest(GeneralResult.Fail("Create your store profile first."));

            if (request.StoreId.HasValue && request.StoreId.Value > 0 && request.StoreId.Value != ownedStore.Id)
                return Forbid();

            request.StoreId = ownedStore.Id;
            request.UserRole = "store_owner";
            return null;
        }

        if (request.StoreId.HasValue && request.StoreId.Value > 0)
            return Forbid();

        request.UserRole = "shopper";
        return null;
    }
}
