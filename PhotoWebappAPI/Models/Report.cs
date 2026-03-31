namespace PhotoWebappAPI.Models
{
    namespace PhotoWebappAPI.Models
    {
        public class Report
        {
            public int Id { get; set; }

            // Người gửi báo cáo
            public string UserId { get; set; }
            public AppUser User { get; set; }

            public string Title { get; set; }
            public string Content { get; set; }
            public bool IsResolved { get; set; } = false;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
