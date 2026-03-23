using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public int Rating { get; set; } // 1 - 5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
