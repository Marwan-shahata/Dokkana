namespace Dokkana.BLL.DTOs.Order
{
    public class OrderItemDTO
    {
        public int? ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    // Full order with line items - used for the order details / confirmation view.
    public class OrderDTO
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PickupCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public IReadOnlyList<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    }

    // Lightweight row for the order-history list.
    public class OrderSummaryDTO
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PickupCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    // Admin view of an order - includes the customer so admins can triage.
    public class OrderAdminSummaryDTO
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PickupCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class UpdateOrderStatusDTO
    {
        // Reserved | Completed | Cancelled
        public string Status { get; set; } = string.Empty;
    }
}
