using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public AppUser Sender { get; set; } = null!;
        public string ReceiverId { get; set; } = string.Empty;
        public AppUser Receiver { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
