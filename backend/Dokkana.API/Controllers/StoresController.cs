using System.Security.Claims;
using Dokkana.BLL.DTOs.Store;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly IStoreManager _storeManager;
        private readonly IAuditLogManager _audit;

        public StoresController(IStoreManager storeManager, IAuditLogManager audit)
        {
            _storeManager = storeManager;
            _audit = audit;
        }

        // Admin: browse all stores (including unapproved).
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var stores = await _storeManager.GetAllAsync();
            return Ok(GeneralResult<IEnumerable<StoreDTO>>.Ok(stores));
        }

        // Public: browse only approved stores.
        [HttpGet("approved")]
        public async Task<IActionResult> GetApproved()
        {
            var stores = await _storeManager.GetApprovedAsync();
            return Ok(GeneralResult<IEnumerable<StoreDTO>>.Ok(stores));
        }

        // StoreOwner: get their own store.
        [HttpGet("my")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> GetMy()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var store = await _storeManager.GetByOwnerAsync(ownerId);
            return store is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<StoreDTO>.Ok(store));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var store = await _storeManager.GetDetailsAsync(id);
            return store is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<StoreDetailsDTO>.Ok(store));
        }

        // A store owner creates their store (pending admin approval).
        [HttpPost]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Create(StoreCreateDTO dto)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var created = await _storeManager.CreateAsync(dto, ownerId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                GeneralResult<StoreDTO>.Ok(created));
        }

        // StoreOwner: update their own store. Admin may update any store.
        [HttpPut("{id:int}")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> Update(int id, StoreUpdateDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _storeManager.UpdateAsync(id, dto, userId, User.IsInRole("Admin"));
            return result switch
            {
                OwnershipResult.Success => Ok(GeneralResult.Ok("Store updated.")),
                OwnershipResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                    GeneralResult.Fail("You do not own this store.")),
                _ => NotFound(GeneralResult.Fail(Errors.NotFound)),
            };
        }

        // Admin: delete a store.
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _storeManager.DeleteAsync(id);
            if (!ok) return NotFound(GeneralResult.Fail(Errors.NotFound));

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "DeleteStore",
                "Store",
                id.ToString());

            return Ok(GeneralResult.Ok("Store deleted."));
        }

        // Admin approves a store so it becomes visible to shoppers.
        [HttpPut("{id:int}/approve")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Approve(int id)
        {
            var ok = await _storeManager.ApproveAsync(id);
            if (!ok) return NotFound(GeneralResult.Fail(Errors.NotFound));

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "ApproveStore",
                "Store",
                id.ToString());

            return Ok(GeneralResult.Ok("Store approved."));
        }
    }
}
