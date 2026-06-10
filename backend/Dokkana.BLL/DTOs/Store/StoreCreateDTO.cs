namespace Dokkana.BLL.DTOs.Store
{
    public class StoreCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
