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
    [Authorize(Roles = "Admin")] // Bảo vệ toàn bộ Controller, chỉ Admin mới được vào
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

        // ==========================================
        // THỐNG KÊ (Dùng cho AdminDashboard)
        // ==========================================
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

        // ==========================================
        // QUẢN LÝ NGƯỜI DÙNG (Dùng cho ManageUsers)
        // ==========================================
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

        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            // Xóa mật khẩu cũ
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded) return BadRequest(new { message = "Lỗi khi xóa mật khẩu cũ." });

            // Đặt lại mật khẩu mới
            var addResult = await _userManager.AddPasswordAsync(user, "Fotoz@123");
            if (!addResult.Succeeded) return BadRequest(new { message = "Lỗi khi đặt mật khẩu mới." });

            return Ok(new { message = "Đã đặt lại mật khẩu về mặc định: Fotoz@123" });
        }

        // ==========================================
        // QUẢN LÝ BÁO CÁO LỖI (Dùng cho ManageReports)
        // ==========================================

        [HttpGet("reports")]
        public async Task<IActionResult> GetAllReports()
        {
            // 🌟 LẤY DATA THẬT TỪ DATABASE
            var reports = await _context.Reports
                .Include(r => r.User) // Kéo theo thông tin người gửi
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    id = r.Id,
                    userName = r.User.FullName,
                    userEmail = r.User.Email,
                    title = r.Title,
                    content = r.Content,
                    isResolved = r.IsResolved,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpPut("reports/{id}/resolve")]
        public async Task<IActionResult> ResolveReport(int id)
        {
            // 🌟 CẬP NHẬT TRẠNG THÁI VÀO DATABASE THẬT
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound("Không tìm thấy báo cáo.");

            report.IsResolved = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã đánh dấu xử lý lỗi thành công!" });
        }
    }
}