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


        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings.FindAsync(id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            // Lấy hết, bao gồm cả thông tin khách và thợ để hiển thị lịch sử cho đẹp
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Photographer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetHistoryByUserIdAsync(string userId, string role)
        {
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Photographer)
                .AsQueryable();

            if (role == "Photographer")
                query = query.Where(b => b.PhotographerId == userId);
            else
                query = query.Where(b => b.CustomerId == userId);

            return await query.OrderByDescending(b => b.Id).ToListAsync();
        }
    }
}
