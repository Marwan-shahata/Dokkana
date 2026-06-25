using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Product
{
    public class ProductCreateDTO
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Count { get; set; }

        [MaxLength(2048)]
        public string? ImageUrl { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int StoreId { get; set; }
    }
}
