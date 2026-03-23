using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.DTOs.Review;
using PhotoWebappAPI.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")] // CHỈ KHÁCH mới được đánh giá thợ
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostReview(CreateReviewDto dto)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sử dụng _context.Bookings trực tiếp với Microsoft.EntityFrameworkCore
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == dto.BookingId && b.CustomerId == customerId);

            if (booking == null) return NotFound("Không tìm thấy đơn hàng.");

            if (booking.Status != "Confirmed")
                return BadRequest("Chỉ đánh giá khi thợ đã nhận việc.");

            var existingReview = await _context.Reviews
                .AnyAsync(r => r.BookingId == dto.BookingId);

            if (existingReview) return BadRequest("Bạn đã đánh giá rồi.");

            var review = new Review
            {
                BookingId = dto.BookingId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow // Đã hết lỗi nếu bạn làm bước 2 ở trên
            };

            _context.Reviews.Add(review);
            booking.Status = "Completed";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đánh giá thành công!" });
        }
    }
}
