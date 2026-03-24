using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Services.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetRequestsFeedAsync();
        Task<IEnumerable<object>> GetPhotographersAsync();
        Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto);
        Task<bool> AcceptBookingAsync(int bookingId, string photographerId);
        Task<bool> RejectBookingAsync(int bookingId, string photographerId);
        Task<bool> CompleteBookingAsync(int bookingId);

        // 🛠️ ĐỔI Ở ĐÂY: Trả về object để khớp với logic mapping ở BookingService
        Task<IEnumerable<object>> GetUserBookingHistoryAsync(string userId, string role);

        Task<bool> CancelBookingAsync(int bookingId, string userId, string role);

    }
}