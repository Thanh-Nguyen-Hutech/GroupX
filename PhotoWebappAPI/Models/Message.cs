using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoWebappAPI.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public AppUser Sender { get; set; } = null!;

        // 🌟 Cho phép null vì chúng ta chat trong phòng (Room) của đơn hàng
        public string? ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public AppUser? Receiver { get; set; }

        // 🌟 2 TRƯỜNG MỚI BẮT BUỘC ĐỂ FOTOZ HOẠT ĐỘNG
        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        // 🌟 Lưu link ảnh từ Cloudinary (nếu có)
        public string? ImageUrl { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}