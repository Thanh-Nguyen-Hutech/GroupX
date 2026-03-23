namespace PhotoWebappAPI.DTOs.Post
{
    public class PostFilterDto
    {
        public string? SearchTerm { get; set; } // Tìm theo tiêu đề hoặc mô tả
        public string? PhotographerName { get; set; } // Tìm theo tên thợ
        public string? SortBy { get; set; } // "newest" hoặc "oldest"
    }
}
