using System.ComponentModel.DataAnnotations;

namespace PhotoWebappAPI.DTOs.Auth
{
    public class RegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Customer"; // Mặc định là Customer, có thể gửi lên "Photographer"
    }
}
