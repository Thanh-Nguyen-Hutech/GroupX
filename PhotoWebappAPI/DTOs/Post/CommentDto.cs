using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.DTOs.Post
{
    public class CommentDto
    {
        [Required(ErrorMessage = "Nội dung bình luận không được để trống")]
        public string Text { get; set; } = string.Empty;
    }
}
