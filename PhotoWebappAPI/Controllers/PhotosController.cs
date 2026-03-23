using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoWebappAPI.Services.Interfaces;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải có Token mới được upload ảnh
    public class PhotosController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            // Kiểm tra xem người dùng có chọn file chưa
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn một bức ảnh để tải lên.");

            // Gọi Service để đẩy ảnh lên Cloudinary
            var result = await _photoService.AddPhotoAsync(file);

            // Nếu Cloudinary báo lỗi (ví dụ: sai key, file quá nặng, định dạng không hỗ trợ...)
            if (result.Error != null)
                return BadRequest(result.Error.Message);

            // Nếu thành công, trả về đường link ảnh cho Front-end
            return Ok(new
            {
                message = "Tải ảnh lên mây thành công!",
                imageUrl = result.SecureUrl.ToString(), // Đường link HTTPS của ảnh
                publicId = result.PublicId              // ID để sau này dùng chức năng Xóa ảnh
            });
        }
    }
}
