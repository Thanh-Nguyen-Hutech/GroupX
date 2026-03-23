using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Repositories.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetRequestsFeedAsync();
        Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto);
    }
}
