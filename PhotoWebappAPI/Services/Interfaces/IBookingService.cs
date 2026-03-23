using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Services.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetRequestsFeedAsync();
        Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto);
        Task<bool> AcceptBookingAsync(int bookingId, string photographerId);
        Task<IEnumerable<Booking>> GetUserBookingHistoryAsync(string userId, string role);
        Task<bool> CancelBookingAsync(int bookingId, string userId, string role);
    }
}
