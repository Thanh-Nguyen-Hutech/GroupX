using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/Users")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPhotoService _photoService;
        private readonly IBookingService _bookingService;

        public UserController(
            UserManager<AppUser> userManager,
            IPhotoService photoService,
            IBookingService bookingService)
        {
            _userManager = userManager;
            _photoService = photoService;
            _bookingService = bookingService;
        }

        // 🌟 NÂNG CẤP: Gộp chung Upload Ảnh và Cập nhật Text vào 1 API
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto) // QUAN TRỌNG: Dùng [FromForm]
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // 1. Xử lý Upload Avatar nếu có file gửi kèm
            if (dto.AvatarFile != null)
            {
                var result = await _photoService.AddPhotoAsync(dto.AvatarFile);
                if (result.Error != null) return BadRequest(result.Error.Message);

                user.Avatar = result.SecureUrl.ToString();
            }

            // 2. Cập nhật các trường thông tin (Chỉ cập nhật nếu có dữ liệu gửi lên)
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;

            // Map Location từ React sang Address trong DB của bạn
            if (!string.IsNullOrEmpty(dto.Location)) user.Address = dto.Location;

            if (!string.IsNullOrEmpty(dto.Bio)) user.Bio = dto.Bio;

            if (dto.BasePrice.HasValue) user.BasePrice = dto.BasePrice.Value;

            // Xử lý mảng Concepts (Lưu dưới dạng chuỗi cách nhau bởi dấu phẩy)
            if (dto.Concepts != null && dto.Concepts.Any())
            {
                user.Concepts = string.Join(",", dto.Concepts);
            }

            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                return Ok(new
                {
                    message = "Cập nhật thông tin thành công!",
                    avatarUrl = user.Avatar
                });
            }

            return BadRequest("Lỗi khi lưu thông tin vào Database.");
        }

        [HttpGet("photographers")]
        [AllowAnonymous] // Cho phép khách chưa đăng nhập cũng xem được danh sách thợ
        public async Task<IActionResult> GetPhotographers()
        {
            var result = await _bookingService.GetPhotographersAsync();
            return Ok(result);
        }

        [HttpGet("profile/{id}")] // Đổi {fullName} thành {id}
        [AllowAnonymous]
        public async Task<IActionResult> GetPhotographerProfile(string id)
        {
            // Dùng ID để tìm kiếm chính xác tuyệt đối 100%
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound("Không tìm thấy nhiếp ảnh gia này.");

            var isPhotographer = await _userManager.IsInRoleAsync(user, "Photographer");
            if (!isPhotographer) return BadRequest("Người dùng này không phải là Nhiếp ảnh gia.");

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                avatar = user.Avatar,
                location = user.Address,
                phoneNumber = user.PhoneNumber,
                bio = user.Bio,
                basePrice = user.BasePrice,
                concepts = string.IsNullOrEmpty(user.Concepts)
                            ? Array.Empty<string>()
                            : user.Concepts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim())
            });
        }
    }

    // 🌟 Lớp DTO được cấu hình để nhận FormData từ React
    public class UpdateProfileDto
    {
        public IFormFile? AvatarFile { get; set; } // File ảnh
        public string? PhoneNumber { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public decimal? BasePrice { get; set; }
        public List<string>? Concepts { get; set; } // Nhận mảng tag (Cá nhân, Cặp đôi...)
    }
}