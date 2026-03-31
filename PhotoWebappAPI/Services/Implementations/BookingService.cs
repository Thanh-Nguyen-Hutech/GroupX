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

        public BookingService(IBookingRepository bookingRepo, UserManager<AppUser> userManager)
        {
            _bookingRepo = bookingRepo;
            _userManager = userManager;
        }

        public async Task<IEnumerable<Booking>> GetRequestsFeedAsync()
        {
            return await _bookingRepo.GetAllPendingAsync();
        }

        // 🌟 ĐÃ NÂNG CẤP: Lưu PhotographerId vào Database nếu có
        public async Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto)
        {
            var newBooking = new Booking
            {
                CustomerId = customerId,
                PhotographerId = dto.PhotographerId, // ✅ Lưu ID thợ nếu đặt trực tiếp
                Title = dto.Title,
                Content = dto.Content,
                ServiceType = dto.ServiceType,
                IncludeMakeup = dto.IncludeMakeup,
                IncludeStudio = dto.IncludeStudio,
                MinPrice = dto.MinPrice,
                MaxPrice = dto.MaxPrice,
                ShootingDate = dto.ShootingDate,
                Location = dto.Location,
                Status = "Pending"
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
            booking.Status = "Accepted";

            return await _bookingRepo.SaveChangesAsync();
        }

        // 🌟 ĐÃ NÂNG CẤP: Trả về Số điện thoại của thợ cho khách hàng
        public async Task<IEnumerable<object>> GetUserBookingHistoryAsync(string userId, string role)
        {
            var bookings = await _bookingRepo.GetHistoryByUserIdAsync(userId, role);

            var result = bookings.Select(b => new {
                id = b.Id,
                customerName = b.Customer?.FullName,
                photographerName = b.Photographer?.FullName,
                phoneNumber = b.Photographer?.PhoneNumber, // ✅ Lấy SĐT của thợ ảnh
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

            if (booking.Status == "Completed" || booking.Status == "Cancelled" || booking.Status == "Rejected")
                return false;

            booking.Status = "Cancelled";
            return await _bookingRepo.SaveChangesAsync();
        }

        public async Task<bool> RejectBookingAsync(int bookingId, string photographerId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || booking.Status != "Pending")
                return false;

            booking.Status = "Rejected";

            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteBookingAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || (booking.Status != "Accepted" && booking.Status != "Confirmed"))
                return false;

            booking.Status = "Completed";

            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        // 🌟 ĐÃ NÂNG CẤP: Map chuẩn dữ liệu ra các thẻ PhotographerCard
        public async Task<IEnumerable<object>> GetPhotographersAsync()
        {
            var photographers = await _userManager.GetUsersInRoleAsync("Photographer");

            return photographers.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                avatar = p.Avatar,
                location = p.Address,
                phoneNumber = p.PhoneNumber,
                basePrice = p.BasePrice,
                concepts = string.IsNullOrEmpty(p.Concepts)
                            ? Array.Empty<string>()
                            : p.Concepts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()),
                rating = 5.0
            });
        }
    }
}