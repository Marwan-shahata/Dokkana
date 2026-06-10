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

        public StoresController(IStoreManager storeManager)
        {
            _storeManager = storeManager;
        }

        // Public: browse stores.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stores = await _storeManager.GetAllAsync();
            return Ok(GeneralResult<IEnumerable<StoreDTO>>.Ok(stores));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var store = await _storeManager.GetByIdAsync(id);
            return store is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<StoreDTO>.Ok(store));
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

        // Admin approves a store so it becomes visible to shoppers.
        [HttpPut("{id:int}/approve")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Approve(int id)
        {
            var ok = await _storeManager.ApproveAsync(id);
            return ok
                ? Ok(GeneralResult.Ok("Store approved."))
                : NotFound(GeneralResult.Fail(Errors.NotFound));
        }
    }
}
