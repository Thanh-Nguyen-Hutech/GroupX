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
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var customers = await _userManager.GetUsersInRoleAsync("Customer");
                var photographers = await _userManager.GetUsersInRoleAsync("Photographer");

                int totalUsers = customers.Count;
                int totalPhotographers = photographers.Count;

                int totalPosts = await _context.Posts.CountAsync();
                int totalBookings = await _context.Bookings.CountAsync();

                int completedBookings = await _context.Bookings
                    .Where(b => b.Status.ToLower() == "completed")
                    .CountAsync();

                string successRate = totalBookings > 0
                    ? $"{(completedBookings * 100.0 / totalBookings):F1}%"
                    : "0%";

                double avgRatingValue = 0;
                if (await _context.Reviews.AnyAsync())
                {
                    avgRatingValue = await _context.Reviews.AverageAsync(r => r.Rating);
                }
                string averageRating = avgRatingValue > 0 ? $"{avgRatingValue:F1}/5.0" : "Chưa có đánh giá";

                bool hasPaymentErrors = await _context.Payments.AnyAsync(p => p.Status == "Failed");
                string paymentStatus = hasPaymentErrors ? "Có giao dịch lỗi" : "Hoạt động ổn định";

                string customerGrowth = "+15.2%";

                return Ok(new
                {
                    totalUsers = totalUsers,
                    totalPhotographers = totalPhotographers,
                    totalPosts = totalPosts,
                    totalBookings = totalBookings,
                    customerGrowth = customerGrowth,
                    bookingSuccessRate = successRate,
                    averageRating = averageRating,
                    paymentStatus = paymentStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tính toán thống kê", error = ex.Message });
            }
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
            // Bỏ qua lỗi nếu User hiện tại đang không có Password
            if (!removeResult.Succeeded && removeResult.Errors.Any(e => e.Code != "PasswordMismatch"))
                return BadRequest(new { message = "Lỗi khi xóa mật khẩu cũ." });

            // Đặt lại mật khẩu mới
            var addResult = await _userManager.AddPasswordAsync(user, "Fotoz@123");
            if (!addResult.Succeeded) return BadRequest(new { message = "Lỗi khi đặt mật khẩu mới." });

            // 🌟 ĐÃ THÊM: Xóa yêu cầu reset mật khẩu của User này (để thông báo biến mất khỏi màn hình React)
            var pendingRequest = await _context.ResetRequests
                .FirstOrDefaultAsync(r => r.Email == user.Email && r.IsProcessed == false);

            if (pendingRequest != null)
            {
                pendingRequest.IsProcessed = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã đặt lại mật khẩu về mặc định: Fotoz@123" });
        }

        // ==========================================
        // YÊU CẦU QUÊN MẬT KHẨU (Dùng cho Notification ManageUsers)
        // ==========================================
        [HttpGet("reset-requests")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingResetRequests()
        {
            try
            {
                // Lấy toàn bộ bản ghi có trường IsProcessed bằng false
                var requests = await _context.ResetRequests
                    .Where(r => r.IsProcessed == false)
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync(); // Lấy danh sách thô về trước để tránh lỗi Linq format ngày tháng

                // Định dạng dữ liệu trả về khớp hoàn toàn với React
                var result = requests.Select(r => new {
                    id = r.Id,
                    email = r.Email,
                    // Hiển thị ngày giờ rõ ràng theo định dạng Việt Nam
                    time = r.RequestedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải yêu cầu.", error = ex.Message });
            }
        }

        // ==========================================
        // QUẢN LÝ BÁO CÁO LỖI (Dùng cho ManageReports)
        // ==========================================
        [HttpGet("reports")]
        public async Task<IActionResult> GetAllReports()
        {
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
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound("Không tìm thấy báo cáo.");

            report.IsResolved = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã đánh dấu xử lý lỗi thành công!" });
        }

        [HttpPost("users/reset-by-email")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPasswordByEmail([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email)) return BadRequest(new { message = "Email không được trống." });

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng sở hữu Email này." });

            // 1. Xóa mật khẩu cũ
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded && removeResult.Errors.Any(e => e.Code != "PasswordMismatch"))
            {
                return BadRequest(new { message = "Lỗi khi xóa mật khẩu cũ hệ thống." });
            }

            // 2. Ép mật khẩu mới
            var addResult = await _userManager.AddPasswordAsync(user, "Fotoz@123");
            if (!addResult.Succeeded) return BadRequest(new { message = "Lỗi khi cập nhật mật khẩu mới." });

            // 3. Đánh dấu hoàn tất toàn bộ yêu cầu của Email này trong DB
            var pendingRequests = await _context.ResetRequests
                .Where(r => r.Email.ToLower() == dto.Email.ToLower() && r.IsProcessed == false)
                .ToListAsync();

            foreach (var req in pendingRequests)
            {
                req.IsProcessed = true;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã đặt lại mật khẩu về mặc định thành công." });
        }
    }
}