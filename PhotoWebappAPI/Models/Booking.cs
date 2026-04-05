using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public AppUser Customer { get; set; } = null!;

        public string? PhotographerId { get; set; } // Nullable vì ban đầu khách có thể đăng tìm thợ chung
        public AppUser? Photographer { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty; // Cá nhân, Cặp đôi, HSSV...
        public DateTime ShootingDate { get; set; }
        public string Location { get; set; } = string.Empty;

        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }

        public bool IncludeMakeup { get; set; }
        public bool IncludeStudio { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        public Review? Review { get; set; }
        public string? GalleryPassword { get; set; } // Mật khẩu xem ảnh
    }

    public class DeliveredPhoto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; }
        public string ImageUrl { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
