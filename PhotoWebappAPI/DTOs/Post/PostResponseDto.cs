namespace PhotoWebappAPI.DTOs.Post
{
    // Khuôn cho bài viết
    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PhotographerId { get; set; }
        // Mình lấy luôn FullName của người thợ để hiển thị cho đẹp
        public string? PhotographerName { get; set; }

        public string? PhotographerAvatar { get; set; }

        // Danh sách các link ảnh đính kèm
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();

        // ✅ THÊM 1: Khoang chứa số lượng Tim
        public int LikesCount { get; set; }

        // ✅ THÊM 2: Khoang chứa danh sách Bình luận
        public List<CommentResponseDto> Comments { get; set; } = new List<CommentResponseDto>();
    }

    // Khuôn cho từng bức ảnh nhỏ bên trong
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    // ✅ THÊM 3: Khuôn cho từng dòng Bình luận trả về React
    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Author { get; set; } = string.Empty; // Tên người bình luận
        public string Text { get; set; } = string.Empty;   // Nội dung chữ
    }
}