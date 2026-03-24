namespace PhotoWebappAPI.DTOs.Auth
{
    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public string? Role { get; set; }

        public string? FullName { get; set; }
    }
}
