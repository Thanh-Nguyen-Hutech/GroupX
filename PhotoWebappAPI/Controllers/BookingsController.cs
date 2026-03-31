using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models; // Chứa AppUser
using PhotoWebappAPI.Services.Interfaces;
using System.Security.Claims;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        // 🌟 BƯỚC 1: Khai báo lại UserManager
        private readonly UserManager<AppUser> _userManager;

        // 🌟 BƯỚC 2: Tiêm (Inject) UserManager vào Constructor để không bị lỗi Compile
        public BookingsController(IBookingService bookingService, UserManager<AppUser> userManager)
        {
            _bookingService = bookingService;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

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

        [HttpPut("{id}/accept")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> AcceptBooking(int id)
        {
            // 🌟 BƯỚC 3: Dùng Email để chọc xuống Database lấy ra đúng cái ID chuẩn 100%
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            // Truyền user.Id xịn vào Service
            var success = await _bookingService.AcceptBookingAsync(id, user.Id);

            if (!success) return BadRequest(new { message = "Không thể nhận Job này (có thể Job đã có người nhận hoặc sai quyền)." });

            return Ok(new { message = "Nhận lịch chụp thành công!" });
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Photographer")]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var result = await _bookingService.RejectBookingAsync(id, user.Id);

            if (result) return Ok(new { message = "Đã từ chối lịch chụp này." });

            return BadRequest(new { message = "Không thể từ chối lịch này." });
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