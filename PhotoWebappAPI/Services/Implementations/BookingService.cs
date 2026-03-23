using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Repositories.Interfaces;
using PhotoWebappAPI.Services.Interfaces;

namespace PhotoWebappAPI.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;

        public BookingService(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        public async Task<IEnumerable<Booking>> GetRequestsFeedAsync()
        {
            // Các logic nghiệp vụ thêm nếu cần (VD: lọc theo ngày)
            return await _bookingRepo.GetAllPendingAsync();
        }

        public async Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto)
        {
            // Mapping từ DTO sang Entity
            var newBooking = new Booking
            {
                CustomerId = customerId,
                Title = dto.Title,
                Content = dto.Content,
                ServiceType = dto.ServiceType,
                IncludeMakeup = dto.IncludeMakeup,
                IncludeStudio = dto.IncludeStudio,
                MinPrice = dto.MinPrice,
                MaxPrice = dto.MaxPrice,
                ShootingDate = dto.ShootingDate,
                Location = dto.Location,
                Status = "Pending" // Mặc định đang tìm thợ
            };

            await _bookingRepo.CreateAsync(newBooking);
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task<bool> AcceptBookingAsync(int bookingId, string photographerId)
        {
            // 1. Dùng Repository để tìm Booking theo Id thay vì dùng _context
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            // 2. Kiểm tra các điều kiện chặn (Validations)
            if (booking == null || booking.Status != "Pending" || !string.IsNullOrEmpty(booking.PhotographerId))
                return false;

            // 3. Cập nhật thông tin thợ nhận việc
            booking.PhotographerId = photographerId;
            booking.Status = "Confirmed";

            // 4. Lưu thay đổi thông qua Repository
            // Lưu ý: Hàm SaveChangesAsync trong Repository nên trả về bool hoặc Task
            return await _bookingRepo.SaveChangesAsync();
        }
        public async Task<IEnumerable<Booking>> GetUserBookingHistoryAsync(string userId, string role)
        {
            // Lấy toàn bộ danh sách từ Repo
            var allBookings = await _bookingRepo.GetAllAsync(); // Đảm bảo Repo có hàm GetAllAsync

            if (role == "Customer")
            {
                return allBookings.Where(b => b.CustomerId == userId)
                                  .OrderByDescending(b => b.ShootingDate);
            }
            else
            {
                return allBookings.Where(b => b.PhotographerId == userId)
                                  .OrderByDescending(b => b.ShootingDate);
            }
        }
        public async Task<bool> CancelBookingAsync(int bookingId, string userId, string role)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null) return false;

            // Kiểm tra quyền sở hữu
            if (role == "Customer" && booking.CustomerId != userId) return false;
            if (role == "Photographer" && booking.PhotographerId != userId) return false;

            // Chỉ cho phép hủy khi chưa hoàn thành
            if (booking.Status == "Completed") return false;

            booking.Status = "Cancelled";

            return await _bookingRepo.SaveChangesAsync();
        }
    }
}
