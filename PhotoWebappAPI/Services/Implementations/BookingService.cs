using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;

        public BookingService(IBookingRepository bookingRepo, UserManager<AppUser> userManager) // 🌟 Inject vào đây
        {
            _bookingRepo = bookingRepo;
            _userManager = userManager;
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
            // ✅ Gọi qua Repository
            var bookings = await _bookingRepo.GetHistoryByUserIdAsync(userId, role);

            // ✅ Thực hiện Mapping tại Service
            var result = bookings.Select(b => new {
                id = b.Id,
                customerName = b.Customer?.FullName,
                photographerName = b.Photographer?.FullName,
                title = b.Title,
                bookingDate = b.ShootingDate,
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

            if (role == "Customer" && booking.CustomerId != userId) return false;
            if (role == "Photographer" && booking.PhotographerId != userId) return false;

            // ✅ Đảm bảo chỉ những đơn CHƯA hoàn thành mới được hủy
            if (booking.Status == "Completed" || booking.Status == "Cancelled" || booking.Status == "Rejected")
                return false;

            booking.Status = "Cancelled";
            return await _bookingRepo.SaveChangesAsync();
        }

        // ✅ ĐÃ SỬA: Dùng _bookingRepo thay cho _context
        public async Task<bool> RejectBookingAsync(int bookingId, string photographerId)
        {
            // Tìm đơn đang ở trạng thái Pending thông qua Repo
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || booking.Status != "Pending")
                return false;

            // Chuyển thành Rejected
            booking.Status = "Rejected";

            // Lưu thay đổi thông qua Repo
            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        // ✅ ĐÃ SỬA: Dùng _bookingRepo thay cho _context
        public async Task<bool> CompleteBookingAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            // ✅ NÂNG CẤP: Chấp nhận cả "Accepted" (mới) và "Confirmed" (cũ)
            if (booking == null || (booking.Status != "Accepted" && booking.Status != "Confirmed"))
                return false;

            // Chuyển thành Completed
            booking.Status = "Completed";

            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        // Đừng quên inject UserManager<AppUser> vào Constructor của Service nếu chưa có
        public async Task<IEnumerable<object>> GetPhotographersAsync()
        {
            // Lấy danh sách user thuộc Role Photographer
            var photographers = await _userManager.GetUsersInRoleAsync("Photographer");

            return photographers.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                avatar = p.Avatar,
                location = p.Address, // Map Address sang location cho khớp Frontend
                basePrice = 1000000, // Bạn có thể thêm cột này vào DB sau, tạm thời để mặc định
                concepts = new[] { "Cá nhân", "Cổ trang" }, // Tạm thời để mảng mẫu
                rating = 5.0
            });
        }
    }
}