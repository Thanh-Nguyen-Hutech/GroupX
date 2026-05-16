using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;
using PhotoWebappAPI.Data;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BookingsController(
            IBookingService bookingService,
            UserManager<AppUser> userManager,
            ApplicationDbContext context)
        {
            _bookingService = bookingService;
            _userManager = userManager;
            _context = context;
        }

        // =========================================================
        // 🌟 TỐI ƯU LUỒNG: TẠO ĐƠN ĐẶT LỊCH (ĐÁP ỨNG CẢ 2 LUỒNG)
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            // TRƯỜNG HỢP 1: KHÁCH ĐẶT ĐÍCH DANH THỢ TỪ PROFILE
            if (!string.IsNullOrEmpty(dto.PhotographerId))
            {
                var booking = new Booking
                {
                    CustomerId = user.Id,
                    PhotographerId = dto.PhotographerId, // Gắn trực tiếp ID thợ
                    Title = dto.Title,
                    Content = dto.Content,
                    ServiceType = dto.ServiceType,
                    Location = dto.Location,
                    MinPrice = dto.MinPrice,
                    MaxPrice = dto.MaxPrice,
                    ShootingDate = dto.ShootingDate,
                    CreatedAt = DateTime.Now, // Đồng bộ thời gian để Worker tự động hủy quét chuẩn
                    Status = "DirectPending" // 🌟 ĐÃ SỬA: Biệt lập luồng đặt đích danh, chờ Thợ Duyệt
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Gửi yêu cầu đặt lịch trực tiếp đến Thợ Ảnh thành công!" });
            }

            // TRƯỜNG HỢP 2: KHÁCH ĐĂNG TÌM THỢ CÔNG KHAI (Chạy qua Service của bạn)
            await _bookingService.CreateBookingRequestAsync(user.Id, dto);
            return Ok(new { message = "Tạo yêu cầu tìm thợ chụp thành công!" });
        }

        [HttpGet("requests-feed")]
        [Authorize(Roles = "Photographer, Admin")]
        public async Task<IActionResult> GetRequestsFeed()
        {
            var requests = await _bookingService.GetRequestsFeedAsync();
            return Ok(requests);
        }

        // =========================================================
        // 🌟 CẬP NHẬT LUỒNG: THỢ CHẤP NHẬN ĐƠN ĐÍCH DANH TỪ PROFILE
        // =========================================================
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> AcceptBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null) return NotFound("Không tìm thấy lịch chụp.");

            // 🌟 ĐÃ CẬP NHẬT: Cho phép thợ duyệt đơn nếu trạng thái là DirectPending (Đơn trực tiếp)
            if (booking.PhotographerId == userId && (booking.Status == "DirectPending" || booking.Status == "WaitingApproval"))
            {
                booking.Status = "Accepted"; // Thợ bấm nhận lịch một cái là CHỐT luôn đơn!
                await _context.SaveChangesAsync();
                return Ok(new { message = "Bạn đã chấp nhận lịch chụp trực tiếp này thành công!" });
            }

            // Fallback chạy luồng nhận Job công khai cũ của bạn thông qua Service
            var success = await _bookingService.AcceptBookingAsync(id, userId);
            if (!success) return BadRequest(new { message = "Không thể nhận Job này." });

            return Ok(new { message = "Nhận lịch chụp thành công!" });
        }

        [HttpPut("{id}/apply")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> ApplyForJob(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null) return NotFound("Không tìm thấy Job.");
            if (booking.Status != "Pending") return BadRequest("Job này không còn mở.");

            // Chuyển trạng thái sang chờ khách duyệt và lưu ID của thợ ứng tuyển công khai
            booking.Status = "WaitingApproval";
            booking.PhotographerId = userId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu! Vui lòng chờ khách hàng duyệt." });
        }

        // LẤY DANH SÁCH THỢ ĐANG CHỜ DUYỆT (Dành cho Khách duyệt Job công khai)
        [HttpGet("my-approvals")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var approvals = await _context.Bookings
                .Include(b => b.Photographer)
                .Where(b => b.CustomerId == userId && b.Status == "WaitingApproval" && b.PhotographerId != null)
                .Select(b => new {
                    BookingId = b.Id,
                    JobTitle = b.Title,
                    PhotographerId = b.PhotographerId,
                    PhotographerName = b.Photographer.FullName,
                    PhotographerAvatar = b.Photographer.Avatar,
                    BasePrice = b.Photographer.BasePrice
                })
                .ToListAsync();

            return Ok(approvals);
        }

        // KHÁCH ĐỒNG Ý THỢ ỨNG TUYỂN (Luồng Job công khai)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ApprovePhotographer(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = "Accepted"; // Chốt Job!
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt thợ thành công!" });
        }

        // KHÁCH TỪ CHỐI THỢ ỨNG TUYỂN (Luồng Job công khai)
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RejectPhotographer(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            // Mở lại Job cho thợ khác ứng tuyển công khai
            booking.Status = "Pending";
            booking.PhotographerId = null;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối thợ." });
        }

        [HttpGet("my-history")]
        [Authorize]
        public async Task<IActionResult> GetMyHistory()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (user == null || string.IsNullOrEmpty(role)) return Unauthorized();

            var history = await _bookingService.GetUserBookingHistoryAsync(user.Id, role);
            return Ok(history);
        }

        [HttpPut("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            var result = await _bookingService.CompleteBookingAsync(id);
            if (result) return Ok(new { message = "Đã xác nhận hoàn thành buổi chụp!" });

            return BadRequest(new { message = "Không thể xác nhận hoàn thành." });
        }

        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (user == null || string.IsNullOrEmpty(role)) return Unauthorized();

            var result = await _bookingService.CancelBookingAsync(id, user.Id, role);
            if (result) return Ok(new { message = "Đã hủy lịch chụp thành công." });

            return BadRequest(new { message = "Không thể hủy đơn hàng này." });
        }
    }
}