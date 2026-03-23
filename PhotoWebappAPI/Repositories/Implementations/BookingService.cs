using PhotoWebappAPI.DTOs.Booking;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Repositories.Interfaces;

namespace PhotoWebappAPI.Repositories.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;

        public BookingService(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        public async Task<IEnumerable<Booking>> GetRequestsFeedAsync()
        {
            // Các logic nghiệp vụ thêm nếu cần (VD: lọc theo ngày)
            return await _bookingRepo.GetAllPendingAsync();
        }

        public async Task CreateBookingRequestAsync(string customerId, CreateBookingDto dto)
        {
            // Mapping từ DTO sang Entity
            var newBooking = new Booking
            {
                CustomerId = customerId,
                Title = dto.Title,
                Content = dto.Content,
                ServiceType = dto.ServiceType,
                IncludeMakeup = dto.IncludeMakeup,
                IncludeStudio = dto.IncludeStudio,
                MinPrice = dto.MinPrice,
                MaxPrice = dto.MaxPrice,
                ShootingDate = dto.ShootingDate,
                Location = dto.Location,
                Status = "Pending" // Mặc định đang tìm thợ
            };

            await _bookingRepo.CreateAsync(newBooking);
            await _bookingRepo.SaveChangesAsync();
        }
    }
}
