using System.ComponentModel.DataAnnotations;

namespace Dokkana.BLL.DTOs.Review
{
    public class ReviewCreateDTO
    {
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
