using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(ApplicationDbContext context, IPhotoService photoService, UserManager<AppUser> userManager)
        {
            _context = context;
            _photoService = photoService;
            _userManager = userManager;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var photographers = await _userManager.GetUsersInRoleAsync("Photographer");
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            return Ok(new
            {
                totalUsers,
                totalPhotographers = photographers.Count,
                totalCustomers = customers.Count,
                totalPosts = await _context.Posts.CountAsync(),
                totalBookings = await _context.Bookings.CountAsync()
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = roles.FirstOrDefault() ?? "Customer",
                    isActive = user.IsActive,
                    lockoutEnd = user.LockoutEnd
                });
            }
            return Ok(result);
        }

        // Đổi tên route thành toggle-lock cho khớp Frontend
        [HttpPost("users/{userId}/toggle-lock")]
        public async Task<IActionResult> ToggleLockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Không cho phép Admin tự khóa chính mình
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == currentUserId) return BadRequest("Bạn không thể tự khóa chính mình.");

            user.IsActive = !user.IsActive;

            if (!user.IsActive)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            else
                await _userManager.SetLockoutEndDateAsync(user, null);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = user.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản thành công",
                    isActive = user.IsActive,
                    lockoutEnd = user.LockoutEnd
                });
            }
            return BadRequest("Lỗi khi cập nhật trạng thái.");
        }
    }
}