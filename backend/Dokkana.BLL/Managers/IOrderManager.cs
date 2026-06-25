using Dokkana.BLL.DTOs.Order;
using Dokkana.Common;

namespace Dokkana.BLL.Managers
{
    public interface IOrderManager
    {
        // Turns the user's current cart into a reservation/order with a pickup code.
        Task<GeneralResult<OrderDTO>> CheckoutAsync(string userId);
        Task<IEnumerable<OrderSummaryDTO>> GetMyOrdersAsync(string userId);
        Task<OrderDTO?> GetByIdAsync(string userId, int orderId);

        // Admin: every order across all users, and status updates.
        Task<IEnumerable<OrderAdminSummaryDTO>> GetAllAsync();
        Task<GeneralResult> UpdateStatusAsync(int orderId, string status);

        // Store owner: orders that include their products, and status updates
        // scoped to orders that contain at least one of their products.
        Task<IEnumerable<StoreOrderDTO>> GetStoreOrdersAsync(string ownerId);
        Task<GeneralResult> UpdateStatusForStoreAsync(int orderId, string status, string ownerId);

        // System: auto-cancel stale reservations and restore their stock.
        Task<int> CancelExpiredReservationsAsync(
            TimeSpan maxReservationAge,
            CancellationToken cancellationToken = default);
    }
}
