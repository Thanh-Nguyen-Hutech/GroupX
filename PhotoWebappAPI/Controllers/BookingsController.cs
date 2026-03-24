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
        [HttpGet("requests-feed")]
        [Authorize(Roles = "Photographer, Admin")]
        public async Task<IActionResult> GetRequestsFeed()
        {
            var requests = await _bookingService.GetRequestsFeedAsync();
            return Ok(requests);
        }

        // PUT: /api/bookings/{id}/accept
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> AcceptBooking(int id)
        {
            var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

            var result = await _bookingService.AcceptBookingAsync(id, photographerId);

            if (result)
                return Ok(new { message = "Chúc mừng! Bạn đã nhận Job này thành công. Hãy liên hệ với khách ngay nhé!" });

            return BadRequest(new { message = "Không thể nhận Job này (có thể Job đã có người nhận hoặc không tồn tại)." });
        }

        // 🌟 THÊM MỚI: PUT: /api/bookings/{id}/reject (Từ chối lịch)
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

            var result = await _bookingService.RejectBookingAsync(id, photographerId);

            if (result)
                return Ok(new { message = "Đã từ chối lịch chụp này." });

            return BadRequest(new { message = "Không thể từ chối lịch này (sai trạng thái hoặc sai quyền)." });
        }

        // GET: /api/bookings/my-history
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

        // 🌟 ĐÃ SỬA: PUT: /api/bookings/{id}/complete (Xác nhận hoàn thành)
        [HttpPut("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            var result = await _bookingService.CompleteBookingAsync(id);

            if (result)
                return Ok(new { message = "Đã xác nhận hoàn thành buổi chụp!" });

            return BadRequest(new { message = "Không thể xác nhận hoàn thành (có thể đơn chưa được nhận hoặc đã hoàn thành rồi)." });
        }

        // PATCH: /api/bookings/{id}/cancel
        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return Unauthorized();

            var result = await _bookingService.CancelBookingAsync(id, userId, role);

            if (result)
                return Ok(new { message = "Đã hủy lịch chụp thành công." });

            return BadRequest(new { message = "Không thể hủy đơn hàng này (Sai quyền hoặc đơn đã hoàn thành)." });
        }
    }
}