using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.DTOs.Post
{
    public class CreatePostDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        // Đổi thành Description cho khớp với Model Post của bạn
        public string? Description { get; set; }

        // Danh sách các file ảnh thợ muốn up lên (cho phép chọn nhiều ảnh)
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
