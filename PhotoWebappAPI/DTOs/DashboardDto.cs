namespace PhotoWebappAPI.DTOs
{
    public class DashboardDto
    {
        public int TotalPhotographers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalBookings { get; set; }
        public double AverageRating { get; set; } // Điểm đánh giá trung bình toàn sàn
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}
