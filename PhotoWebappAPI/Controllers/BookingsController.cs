using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // POST: /api/bookings
        // Chỉ Customer mới được tạo lịch
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();

            await _bookingService.CreateBookingRequestAsync(currentUserId, dto);

            return Ok(new { message = "Tạo yêu cầu tìm thợ chụp thành công!" });
        }

        // GET: /api/bookings/requests-feed
        // Lấy danh sách cho Thợ lướt xem và ứng tuyển
        [HttpGet("requests-feed")]
        [Authorize(Roles = "Photographer, Admin")]
        public async Task<IActionResult> GetRequestsFeed()
        {
            var requests = await _bookingService.GetRequestsFeedAsync();
            return Ok(requests);
        }

        // PUT: /api/bookings/{id}/accept
        // Thợ nhận job
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> AcceptBooking(int id)
        {
            var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

            var result = await _bookingService.AcceptBookingAsync(id, photographerId);

            if (result)
            {
                return Ok(new { message = "Chúc mừng! Bạn đã nhận Job này thành công. Hãy liên hệ với khách ngay nhé!" });
            }

            return BadRequest("Không thể nhận Job này (có thể Job đã có người nhận hoặc không tồn tại).");
        }

        // GET: /api/bookings/my-history
        // Xem lịch sử đặt/nhận chụp
        [HttpGet("my-history")]
        [Authorize]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized();

            var history = await _bookingService.GetUserBookingHistoryAsync(userId, role);

            return Ok(history);
        }

        // PATCH: /api/bookings/{id}/cancel
        // Hủy đơn
        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return Unauthorized();

            var result = await _bookingService.CancelBookingAsync(id, userId, role);

            if (result)
            {
                return Ok(new { message = "Đã hủy đơn hàng thành công." });
            }

            return BadRequest("Không thể hủy đơn hàng này (Sai quyền hoặc đơn đã hoàn thành).");
        }
    }
}