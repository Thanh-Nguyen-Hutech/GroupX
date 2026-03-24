using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Implementations;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    // ✅ Đã sửa Route thành "Users" (số nhiều) để khớp với Axios bên React
    [Route("api/Users")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPhotoService _photoService;
        private readonly IBookingService _bookingService; // 🌟 Thêm dòng này

        public UserController(
            UserManager<AppUser> userManager,
            IPhotoService photoService,
            IBookingService bookingService) // 🌟 Inject vào đây
        {
            _userManager = userManager;
            _photoService = photoService;
            _bookingService = bookingService;
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var result = await _photoService.AddPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);

            user.Avatar = result.SecureUrl.ToString();
            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                return Ok(new
                {
                    message = "Cập nhật ảnh đại diện thành công!",
                    avatarUrl = user.Avatar
                });
            }

            return BadRequest("Lỗi khi lưu vào Database.");
        }

        // 🌟 NÂNG CẤP: Thêm API Cập nhật Profile
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            // Tìm User hiện tại thông qua Email lưu trong Token
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Cập nhật các trường thông tin
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.Bio = dto.Bio;

            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                return Ok(new { message = "Cập nhật thông tin thành công!" });
            }

            return BadRequest("Lỗi khi lưu thông tin vào Database.");
        }

        [HttpGet("photographers")]
        [AllowAnonymous] // Cho phép khách chưa đăng nhập cũng xem được danh sách thợ
        public async Task<IActionResult> GetPhotographers()
        {
            // Gọi qua Service để lấy dữ liệu
            var result = await _bookingService.GetPhotographersAsync();
            return Ok(result);
        }
    }

    // 🌟 Lớp DTO để nhận dữ liệu từ React gửi lên
    // (Bạn có thể để tạm ở đây hoặc move sang folder DTOs cho gọn)
    public class UpdateProfileDto
    {
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Bio { get; set; }
    }

}