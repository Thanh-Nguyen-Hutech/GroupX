using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using PhotoWebappAPI.Helpers;
using PhotoWebappAPI.Services.Interfaces;

namespace PhotoWebappAPI.Services.Implementations
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        // Bơm (Inject) cấu hình từ appsettings.json vào đây
        public PhotoService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        // 🌟 ĐÃ FIX: Hàm up ảnh cho bài đăng (Portfolio) không còn bị crop 500x500
        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Chỉ tối ưu dung lượng và định dạng, giữ nguyên kích thước và tỷ lệ gốc!
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
                    Folder = "PhotoApp_Portfolios" // Tự tạo folder trên mây để dễ quản lý
                };

                // Đẩy lên mây
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }

        // Hàm này chuyên dùng để up ảnh gốc, ảnh HD giao cho khách, không cắt xén!
        public async Task<ImageUploadResult> AddGalleryPhotoAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Giữ nguyên chất lượng (quality: auto:good) cho ảnh HD
                    Transformation = new Transformation().Quality("auto:good"),
                    Folder = "PhotoApp_ClientGalleries" // Lưu vào một folder riêng biệt
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
        }
    }
}