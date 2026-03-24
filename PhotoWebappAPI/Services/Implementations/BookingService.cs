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
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || booking.Status != "Pending" || !string.IsNullOrEmpty(booking.PhotographerId))
                return false;

            booking.PhotographerId = photographerId;
            booking.Status = "Accepted"; // ✅ ĐỔI TỪ Confirmed THÀNH Accepted

            return await _bookingRepo.SaveChangesAsync();
        }
        public async Task<IEnumerable<object>> GetUserBookingHistoryAsync(string userId, string role)
        {
            // ✅ Gọi qua Repository thay vì _context
            var bookings = await _bookingRepo.GetHistoryByUserIdAsync(userId, role);

            // ✅ Thực hiện Mapping tại Service
            var result = bookings.Select(b => new {
                id = b.Id,
                // Logic: Nếu tôi là Thợ, hiện tên Khách. Nếu tôi là Khách, hiện tên Thợ.
                customerName = b.Customer?.FullName,
                photographerName = b.Photographer?.FullName,
                title = b.Title,
                bookingDate = b.ShootingDate, // Đồng bộ tên biến cho Frontend
                location = b.Location,
                serviceType = b.ServiceType,
                notes = b.Content,
                status = b.Status,
                minPrice = b.MinPrice,
                maxPrice = b.MaxPrice
            });

            return result;
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
