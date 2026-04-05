using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService; // 🌟 Thêm service ảnh vào đây

        public GalleryController(ApplicationDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        // API Kiểm tra mật khẩu và trả về danh sách ảnh
        // Đừng quên dòng này ở đầu file nhé (nếu chưa có)
        // using Microsoft.EntityFrameworkCore;

        [HttpPost("{bookingId}/verify")]
        public async Task<IActionResult> VerifyAndGetPhotos(int bookingId, [FromBody] string password)
        {
            // Dùng .Include() để lấy kèm thông tin AppUser (Thợ ảnh)
            var booking = await _context.Bookings
                .Include(b => b.Photographer)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound("Không tìm thấy đơn hàng");

            // Kiểm tra mật khẩu
            if (string.IsNullOrEmpty(booking.GalleryPassword) || booking.GalleryPassword != password)
            {
                return Unauthorized("Mật khẩu không chính xác!");
            }

            // Mật khẩu đúng -> Lấy danh sách ảnh
            var photos = await _context.DeliveredPhotos
                .Where(p => p.BookingId == bookingId)
                .Select(p => p.ImageUrl)
                .ToListAsync();

            return Ok(new
            {
                Message = "Xác thực thành công",
                // Tùy thuộc vào AppUser của bạn có trường FullName hay UserName, bạn hãy chọn tên cho đúng nhé!
                // Ở đây mình ví dụ gọi trường UserName, nếu bạn dùng FullName thì đổi thành b.Photographer.FullName
                PhotographerName = booking.Photographer != null ? booking.Photographer.UserName : "Thợ ảnh",
                Photos = photos
            });
        }

        [HttpPost("{bookingId}/upload")]
        public async Task<IActionResult> UploadGallery(int bookingId, [FromForm] List<IFormFile> files)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Không tìm thấy đơn đặt lịch này.");

            // 1. Tạo mật khẩu nếu chưa có
            if (string.IsNullOrEmpty(booking.GalleryPassword))
            {
                const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                var random = new Random();
                booking.GalleryPassword = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            if (files == null || files.Count == 0) return BadRequest("Vui lòng chọn ảnh.");

            // 2. Upload từng file qua Cloudinary bằng HÀM MỚI TẠO
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // 🌟 Gọi hàm up ảnh giữ nguyên gốc
                    var result = await _photoService.AddGalleryPhotoAsync(file);

                    if (result.Error != null) return BadRequest(result.Error.Message);

                    var newPhoto = new DeliveredPhoto
                    {
                        BookingId = bookingId,
                        ImageUrl = result.SecureUrl.ToString(), // 🌟 Lấy link an toàn (https)
                        UploadedAt = DateTime.Now
                    };
                    _context.DeliveredPhotos.Add(newPhoto);
                }
            }

            booking.Status = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Giao ảnh thành công!",
                Password = booking.GalleryPassword
            });
        }
    }
}
