using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.DTOs.Booking
{
    public class CreateBookingDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [Required]
        public string ServiceType { get; set; } = string.Empty; // Cá nhân, Cặp đôi...

        public bool IncludeMakeup { get; set; }
        public bool IncludeStudio { get; set; }

        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }

        [Required]
        public DateTime ShootingDate { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string? PhotographerId { get; set; }
    }
}
