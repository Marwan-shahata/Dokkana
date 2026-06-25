namespace Dokkana.BLL.DTOs.AI;

public class AiSearchIntentDto
{
    public string RawQuery { get; set; } = string.Empty;

    public string? NormalizedArabic { get; set; }

    public string? NormalizedEnglish { get; set; }

    public List<string> SearchTerms { get; set; } = new();

    public string? ProductType { get; set; }

    public string? Category { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? Gender { get; set; }

    public string? Brand { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string LanguageDetected { get; set; } = "mixed";
}