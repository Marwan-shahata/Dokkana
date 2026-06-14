using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Category
{
    public class CategoryCreateDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }
    }
}
