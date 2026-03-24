namespace PhotoWebappAPI.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace PhotoWebappAPI.Models
    {
        public class Like
        {
            [Key]
            public int Id { get; set; }

            public int PostId { get; set; }
            [ForeignKey("PostId")]
            public Post Post { get; set; }

            public string UserId { get; set; }
            [ForeignKey("UserId")]
            public AppUser User { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
