using Microsoft.AspNetCore.Identity;

namespace PhotoWebappAPI.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public string? Address { get; set; }
        public string? FacebookUrl { get; set; }
        public bool IsActive { get; set; } = true; // Mặc định tài khoản mới là Active

        // Navigation properties
        public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
        public ICollection<Booking> PhotographerBookings { get; set; } = new List<Booking>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}
