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
        private readonly IPhotoService _photoService;

        public GalleryController(ApplicationDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        [HttpPost("{bookingId}/verify")]
        public async Task<IActionResult> VerifyAndGetPhotos(int bookingId, [FromBody] string password)
        {
            var booking = await _context.Bookings
                .Include(b => b.Photographer)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound("Không tìm thấy đơn hàng");

            if (string.IsNullOrEmpty(booking.GalleryPassword) || booking.GalleryPassword != password)
            {
                return Unauthorized("Mật khẩu không chính xác!");
            }

            // 🌟 NÂNG CẤP: Lấy cả Id của ảnh để frontend có thể gọi API xóa
            var photos = await _context.DeliveredPhotos
                .Where(p => p.BookingId == bookingId)
                .Select(p => new {
                    Id = p.Id,
                    Url = p.ImageUrl
                })
                .ToListAsync();

            return Ok(new
            {
                Message = "Xác thực thành công",
                PhotographerName = booking.Photographer != null ? booking.Photographer.FullName : "Thợ ảnh",
                Photos = photos
            });
        }

        [HttpPost("{bookingId}/upload")]
        public async Task<IActionResult> UploadGallery(int bookingId, [FromForm] List<IFormFile> files)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Không tìm thấy đơn đặt lịch này.");

            if (string.IsNullOrEmpty(booking.GalleryPassword))
            {
                const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                var random = new Random();
                booking.GalleryPassword = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            if (files == null || files.Count == 0) return BadRequest("Vui lòng chọn ảnh.");

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var result = await _photoService.AddGalleryPhotoAsync(file);
                    if (result.Error != null) return BadRequest(result.Error.Message);

                    var newPhoto = new DeliveredPhoto
                    {
                        BookingId = bookingId,
                        ImageUrl = result.SecureUrl.ToString(),
                        // PublicId = result.PublicId, // 💡 Nếu Database của bạn có cột PublicId để xóa Cloudinary thì mở comment dòng này
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

        // 🌟 TÍNH NĂNG MỚI: API Xóa ảnh lẻ
        [HttpDelete("photo/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var photo = await _context.DeliveredPhotos.FindAsync(photoId);
            if (photo == null) return NotFound(new { message = "Không tìm thấy ảnh này." });

            // 💡 Nếu service của bạn có hàm xóa trên Cloudinary, hãy gọi nó ở đây. 
            // Ví dụ: await _photoService.DeletePhotoAsync(photo.PublicId);

            _context.DeliveredPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa ảnh thành công khỏi kho lưu trữ." });
        }
    }
}