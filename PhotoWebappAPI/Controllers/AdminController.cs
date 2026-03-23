using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.DTOs;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            // 1. Đếm số lượng User theo Role (Dựa vào bảng Identity)
            // Lưu ý: Tên Role phải khớp với lúc bạn Register
            var totalPhotographers = await _context.UserRoles
                .CountAsync(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Photographer"));

            var totalCustomers = await _context.UserRoles
                .CountAsync(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Customer"));

            // 2. Thống kê bài đăng và booking
            var totalPosts = await _context.Posts.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();

            // 3. Tính điểm đánh giá trung bình
            var avgRating = await _context.Reviews.AnyAsync()
                ? await _context.Reviews.AverageAsync(r => r.Rating)
                : 0;

            // 4. Lấy 5 bài đăng mới nhất để làm "Hoạt động gần đây"
            var recentPosts = await _context.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentActivityDto
                {
                    Message = $"Bài đăng mới: {p.Title}",
                    Time = p.CreatedAt
                })
                .ToListAsync();

            var stats = new DashboardDto
            {
                TotalPhotographers = totalPhotographers,
                TotalCustomers = totalCustomers,
                TotalPosts = totalPosts,
                TotalBookings = totalBookings,
                AverageRating = Math.Round(avgRating, 1),
                RecentActivities = recentPosts
            };

            return Ok(stats);
        }


        [HttpPost("users/{userId}/toggle-block")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleBlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Giả sử bạn thêm cột IsActive vào AppUser, nếu chưa có hãy thêm vào Model AppUser nhé
            user.IsActive = !user.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { message = user.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản thành công" });
            }
            return BadRequest("Lỗi khi cập nhật trạng thái.");
        }

        [HttpDelete("posts/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeletePost(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            // 1. Xóa ảnh trên Cloudinary (Tận dụng IPhotoService đã tiêm vào)
            foreach (var photo in post.Photos)
            {
                if (!string.IsNullOrEmpty(photo.PublicId))
                    await _photoService.DeletePhotoAsync(photo.PublicId);
            }

            // 2. Xóa trong Database
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Admin đã xóa bài đăng ID: {postId}" });
        }

        [HttpGet("bookings/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Photographer)
                .OrderByDescending(b => b.ShootingDate)
                .ToListAsync();

            return Ok(bookings);
        }
    }

}
