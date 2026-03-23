using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        public string PhotographerId { get; set; } = string.Empty;
        public AppUser Photographer { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}
