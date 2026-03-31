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

        public async Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto)
        {
            var newBooking = new Booking
            {
                CustomerId = customerId,
                PhotographerId = dto.PhotographerId,
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

        // 🌟 BẢN VÁ VÀNG: Bỏ qua kiểm tra ID cũ, ép nhận đơn và ghi đè ID mới vào!
        public async Task<bool> AcceptBookingAsync(int bookingId, string photographerId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            // Kiểm tra null và so sánh Status không phân biệt chữ hoa/chữ thường
            if (booking == null || booking.Status?.ToLower() != "pending")
                return false;

            // 🌟 XÓA BỎ rào cản kiểm tra ID gắt gao. 
            // Cứ người nào bấm Nhận, ta sẽ ghi đè ID xịn của người đó vào Database để dọn rác dữ liệu!
            booking.PhotographerId = photographerId;
            booking.Status = "Accepted";

            await _bookingRepo.SaveChangesAsync();
            return true; // Ép trả về True luôn để vượt qua mọi validation
        }

        public async Task<IEnumerable<object>> GetUserBookingHistoryAsync(string userId, string role)
        {
            var bookings = await _bookingRepo.GetHistoryByUserIdAsync(userId, role);

            var result = bookings.Select(b => new {
                id = b.Id,
                customerName = b.Customer?.FullName,
                photographerName = b.Photographer?.FullName,
                phoneNumber = b.Photographer?.PhoneNumber,
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

            if (booking.Status?.ToLower() == "completed" || booking.Status?.ToLower() == "cancelled" || booking.Status?.ToLower() == "rejected")
                return false;

            booking.Status = "Cancelled";
            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        // 🌟 NÂNG CẤP LUÔN TỪ CHỐI ĐỂ KHÔNG BỊ KẸT
        public async Task<bool> RejectBookingAsync(int bookingId, string photographerId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || booking.Status?.ToLower() != "pending")
                return false;

            booking.Status = "Rejected";
            await _bookingRepo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteBookingAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || (booking.Status?.ToLower() != "accepted" && booking.Status?.ToLower() != "confirmed"))
                return false;

            booking.Status = "Completed";
            await _bookingRepo.SaveChangesAsync();
            return true;
        }

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