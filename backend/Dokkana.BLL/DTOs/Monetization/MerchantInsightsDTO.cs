namespace Dokkana.BLL.DTOs.Monetization
{
    // Pro-only merchant insights (MM3): demand forecast + pricing hints.
    public class MerchantInsightsDTO
    {
        public bool IsPro { get; set; }
        public List<DemandForecastDTO> Forecasts { get; set; } = new();
        public List<PricingHintDTO> PricingHints { get; set; } = new();
    }

    // "Likely to sell out of X in ~N days" at the recent sales rate.
    public class DemandForecastDTO
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int UnitsSold30d { get; set; }
        public int? DaysToSellout { get; set; }
        public string Severity { get; set; } = "ok"; // urgent | soon | ok
    }

    // "Similar items nearby sell for L–H; you're at P."
    public class PricingHintDTO
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal MarketLow { get; set; }
        public decimal MarketHigh { get; set; }
        public decimal MarketMedian { get; set; }
        public string Verdict { get; set; } = "fair"; // high | low | fair
    }
}
