using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllPendingAsync();
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id); // Thêm dòng này
        Task CreateAsync(Booking booking);
        Task<bool> SaveChangesAsync(); // Thêm dòng này
        Task<IEnumerable<Booking>> GetHistoryByUserIdAsync(string userId, string role);

    }
}
