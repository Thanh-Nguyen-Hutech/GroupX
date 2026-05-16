using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllPendingAsync();
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id); 
        Task CreateAsync(Booking booking);
        Task<bool> SaveChangesAsync();
        Task<IEnumerable<Booking>> GetHistoryByUserIdAsync(string userId, string role);
    }
}
