using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.DTOs.Review
{
    public class CreateReviewDto
    {
        [Required]
        public int BookingId { get; set; } // Đánh giá cho đơn hàng nào?

        [Range(1, 5, ErrorMessage = "Số sao từ 1 đến 5")]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
