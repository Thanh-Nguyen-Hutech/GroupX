using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class ResetRequest
    {
        [Key]
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false; // Đánh dấu đã xử lý hay chưa
    }
}