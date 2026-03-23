using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Repositories.Interfaces;
namespace PhotoWebappAPI.Repositories.Implementations
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllPendingAsync()
        {
            return await _context.Bookings
                .Include(b => b.Customer) // Join bảng để lấy info người đăng
                .Where(b => b.Status == "Pending")
                .OrderByDescending(b => b.ShootingDate)
                .ToListAsync();
        }

        public async Task CreateAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
