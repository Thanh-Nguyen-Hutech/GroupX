using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllPendingAsync();
        Task CreateAsync(Booking booking);
        Task SaveChangesAsync();
    }
}
