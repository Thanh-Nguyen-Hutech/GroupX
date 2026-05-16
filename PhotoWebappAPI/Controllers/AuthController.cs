using Microsoft.AspNetCore.Authorization; // 🌟 Để dùng thuộc tính [Authorize]
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.DTOs.Auth;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims; // 🌟 Để đọc Claim dữ liệu từ Token
using PhotoWebappAPI.Data;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _authService = authService;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);
            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Message });

            return Ok(result);
        }

        // =========================================================
        // LUỒNG CHƯA ĐĂNG NHẬP: QUÊN MẬT KHẨU (BÁO CÁO ĐẾN ADMIN)
        // =========================================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var request = new ResetRequest
                {
                    Email = dto.Email,
                    RequestedAt = DateTime.Now,
                    IsProcessed = false
                };

                _context.ResetRequests.Add(request);
                await _context.SaveChangesAsync();

                Console.WriteLine($"\n===========================================");
                Console.WriteLine($"[DATABASE SAVED] Đã lưu yêu cầu của: {dto.Email}");
                Console.WriteLine($"===========================================\n");
            }

            return Ok(new
            {
                message = "Yêu cầu khôi phục đã được gửi đến Admin thành công!"
            });
        }

        // =========================================================
        // LUỒNG ĐÃ ĐĂNG NHẬP: NGƯỜI DÙNG CHỦ ĐỘNG ĐỔI MẬT KHẨU TRONG PROFILE
        // =========================================================
        [HttpPost("change-password")]
        [Authorize] // 🛡️ Chỉ cho phép tài khoản đã đăng nhập (có gắn kèm JWT Token) gọi API này
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại!" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy thông tin tài khoản người dùng." });

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return BadRequest(new { message = firstError ?? "Cập nhật mật khẩu thất bại!" });
            }

            return Ok(new { message = "Mật khẩu của bạn đã được thay đổi thành công!" });
        }

        // =========================================================
        // 🌟 LUỒNG TÍCH HỢP FORM LOGIN: ĐỔI MẬT KHẨU QUA EMAIL (KHÔNG CẦN TOKEN)
        // =========================================================
        [HttpPost("change-password-by-email")]
        public async Task<IActionResult> ChangePasswordByEmail([FromBody] ChangePasswordByEmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Tìm user theo Email được gửi lên từ Form Login
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { message = "Địa chỉ Email hoặc mật khẩu hiện tại không chính xác!" });

            // 2. Thực hiện đổi mật khẩu bằng API tích hợp sẵn của Identity UserManager
            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return BadRequest(new { message = firstError ?? "Mật khẩu hiện tại không đúng hoặc mật khẩu mới chưa đủ độ phức tạp!" });
            }

            return Ok(new { message = "Mật khẩu của bạn đã được cập nhật thành công!" });
        }
    }

    // DTO cho tính năng Quên mật khẩu
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }

    // DTO cho tính năng Đổi mật khẩu trong Profile (Cần token đăng nhập)
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có độ dài tối thiểu từ 6 ký tự")]
        public string NewPassword { get; set; }
    }

    // 🌟 ĐÃ THÊM: DTO nhận dữ liệu cho tính năng Đổi mật khẩu trực tiếp tại Form Login bằng Email
    public class ChangePasswordByEmailDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có độ dài tối thiểu từ 6 ký tự")]
        public string NewPassword { get; set; }
    }
}