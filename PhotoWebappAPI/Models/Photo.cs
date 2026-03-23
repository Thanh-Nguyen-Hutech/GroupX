using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.Models
{
    public class Photo
    {
        [Key]

        public int Id { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
        public string Url { get; set; } = string.Empty; // Lưu URL từ Cloudinary/Firebase
        public string? PublicId { get; set; }
    }
}
