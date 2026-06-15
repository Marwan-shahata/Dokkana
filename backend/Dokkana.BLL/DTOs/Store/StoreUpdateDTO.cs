using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Store
{
    public class StoreUpdateDTO
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? LogoUrl { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
