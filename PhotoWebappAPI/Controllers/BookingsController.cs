using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Repositories.Interfaces;
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

            // Lấy ID của người dùng đang đăng nhập từ JWT Token
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();

            await _bookingService.CreateBookingRequestAsync(currentUserId, dto);

            return Ok(new { message = "Tạo yêu cầu tìm thợ chụp thành công!" });
        }

        // GET: /api/bookings/requests-feed
        // Lấy danh sách cho Thợ lướt xem và ứng tuyển
        [HttpGet("requests-feed")]
        [Authorize(Roles = "Photographer, Admin")] // Thợ hoặc Admin mới được xem
        public async Task<IActionResult> GetRequestsFeed()
        {
            var requests = await _bookingService.GetRequestsFeedAsync();
            // LƯU Ý: Trong thực tế nên map requests (Entity) ra một DTO khác để không lộ ID dư thừa
            return Ok(requests);
        }
    }
}
