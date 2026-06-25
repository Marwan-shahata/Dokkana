using System.Security.Claims;
using Dokkana.BLL.DTOs.Order;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderManager _orderManager;
        private readonly IAuditLogManager _audit;
        private readonly INotificationManager _notifications;

        public OrdersController(IOrderManager orderManager, IAuditLogManager audit, INotificationManager notifications)
        {
            _orderManager = orderManager;
            _audit = audit;
            _notifications = notifications;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Reserve/checkout the current cart and receive a pickup code.
        [HttpPost("checkout")]
        [Authorize(Policy = "ShopperOnly")]
        public async Task<IActionResult> Checkout()
        {
            var result = await _orderManager.CheckoutAsync(UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Current user's order history.
        [HttpGet]
        [Authorize(Policy = "ShopperOnly")]
        public async Task<IActionResult> GetMine()
        {
            var orders = await _orderManager.GetMyOrdersAsync(UserId);
            return Ok(GeneralResult<IEnumerable<OrderSummaryDTO>>.Ok(orders));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "ShopperOnly")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderManager.GetByIdAsync(UserId, id);
            return order is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<OrderDTO>.Ok(order));
        }

        // Admin

        // All orders across every user (for the admin dashboard).
        [HttpGet("all")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orderManager.GetAllAsync();
            return Ok(GeneralResult<IEnumerable<OrderAdminSummaryDTO>>.Ok(orders));
        }

        // Admin updates an order's status (Reserved | Completed | Cancelled).
        [HttpPut("{id:int}/status")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDTO dto)
        {
            var result = await _orderManager.UpdateStatusAsync(id, dto.Status);
            if (!result.Success) return BadRequest(result);

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "UpdateOrderStatus",
                "Order",
                id.ToString(),
                dto.Status);

            // Let the shopper know their order status changed.
            await _notifications.NotifyOrderStatusChangedAsync(id, dto.Status);

            return Ok(result);
        }

        // Store owner

        // Orders that include the signed-in owner's products (filtered to their lines).
        [HttpGet("store")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> GetStoreOrders()
        {
            var orders = await _orderManager.GetStoreOrdersAsync(UserId);
            return Ok(GeneralResult<IEnumerable<StoreOrderDTO>>.Ok(orders));
        }

        // Store owner updates the status of an order that contains their products.
        [HttpPut("store/{id:int}/status")]
        [Authorize(Policy = "StoreOwnerOnly")]
        public async Task<IActionResult> UpdateStoreStatus(int id, UpdateOrderStatusDTO dto)
        {
            var result = await _orderManager.UpdateStatusForStoreAsync(id, dto.Status, UserId);
            if (!result.Success) return BadRequest(result);

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "UpdateOrderStatus(Store)",
                "Order",
                id.ToString(),
                dto.Status);

            // Let the shopper know their order status changed.
            await _notifications.NotifyOrderStatusChangedAsync(id, dto.Status);

            return Ok(result);
        }
    }
}
