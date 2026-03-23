namespace PhotoWebappAPI.DTOs.Post
{
    // Khuôn cho bài viết
    public class PostResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Mình lấy luôn FullName của người thợ để hiển thị cho đẹp
        public string? PhotographerName { get; set; }

        // Danh sách các link ảnh đính kèm
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
    }

    // Khuôn cho từng bức ảnh nhỏ bên trong
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
