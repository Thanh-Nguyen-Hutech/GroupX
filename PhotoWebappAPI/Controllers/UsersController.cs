using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPhotoService _photoService;

        public UserController(UserManager<AppUser> userManager, IPhotoService photoService)
        {
            _userManager = userManager;
            _photoService = photoService;
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            // 1. Tự động nhận diện ai đang đăng nhập dựa vào Token
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return Unauthorized("Token không hợp lệ.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // 2. Đẩy file ảnh lên Cloudinary
            var result = await _photoService.AddPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);

            // 3. Gắn link URL vừa lấy được vào cột Avatar của User
            user.Avatar = result.SecureUrl.ToString();

            // 4. Lưu xuống Database
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
    }
}
