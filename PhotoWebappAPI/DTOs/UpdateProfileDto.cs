namespace PhotoWebappAPI.DTOs
{
    public class UpdateProfileDto
    {
        public IFormFile? AvatarFile { get; set; }
        public decimal? BasePrice { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public List<string>? Concepts { get; set; }
    }
}
