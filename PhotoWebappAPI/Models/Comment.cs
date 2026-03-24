using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoWebappAPI.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
