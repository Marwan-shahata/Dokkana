namespace Dokkana.BLL.DTOs.AI.Copilot;

public class SalesSummaryDto
{
    public int TotalSoldItems { get; set; }

    public int TotalSaleRecords { get; set; }

    public decimal TotalRevenue { get; set; }

    public int DifferentProductsSold { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }
}