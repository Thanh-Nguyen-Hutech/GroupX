using CloudinaryDotNet.Actions;

namespace PhotoWebappAPI.Services.Interfaces
{
    public interface IPhotoService
    {
        // Hàm nhận file từ Client và trả về kết quả từ Cloudinary
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);

        // Hàm xóa ảnh trên Cloudinary dựa vào ID của ảnh
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}
