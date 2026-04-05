using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetChatHistory(int bookingId)
        {
            // Lấy toàn bộ tin nhắn của Booking này, Join với bảng User để lấy Tên
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.BookingId == bookingId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    senderName = m.Sender != null ? m.Sender.FullName : "Khách ẩn danh",
                    message = m.Content,
                    imageUrl = m.ImageUrl
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}