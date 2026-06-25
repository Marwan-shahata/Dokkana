namespace Dokkana.BLL.DTOs.AI.Copilot;

public class BusinessIntentDto
{
    public string Intent { get; set; } = "sales_summary";

    public string Period { get; set; } = "this_month";

    public int Limit { get; set; } = 5;

    public int LowStockThreshold { get; set; } = 5;
}