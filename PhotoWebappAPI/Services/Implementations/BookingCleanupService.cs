using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoWebappAPI.Services
{
    public class BookingCleanupService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public BookingCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Thiết lập chạy định kỳ: Cứ sau mỗi 1 tiếng (60 phút) thì quét DB 1 lần
            _timer = new Timer(DoCleanupWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private async void DoCleanupWork(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    // 🌟 CẬP NHẬT LOGIC: Nếu lịch vẫn ở trạng thái "WaitingApproval" (Chờ duyệt) 
                    // nhưng ngày hẹn chụp (ShootingDate) đã qua so với thời gian hiện tại của hệ thống,
                    // đơn đó sẽ bị coi là quá hạn và cần hủy tự động.
                    var expiredBookings = await context.Bookings
                        .Where(b => (b.Status.ToLower() == "waitingapproval" || b.Status.ToLower() == "directpending") && b.ShootingDate < DateTime.Now)
                        .ToListAsync();

                    if (expiredBookings.Any())
                    {
                        foreach (var booking in expiredBookings)
                        {
                            booking.Status = "Cancelled";

                            // 🌟 ĐÃ SỬA: Đổi từ .Notes sang .Content cho khớp 100% với Model Booking của bác
                            booking.Content += " [Hệ thống tự động hủy đơn do đã quá thời gian hẹn chụp mà không có phản hồi duyệt/nhận lịch]";
                        }

                        await context.SaveChangesAsync();
                        Console.WriteLine($"\n==================================================================");
                        Console.WriteLine($"[BACKGROUND WORKER] Đã tự động giải phóng {expiredBookings.Count} đơn đặt lịch quá hạn!");
                        Console.WriteLine($"==================================================================\n");
                    }
                }
                catch (Exception ex)
                {
                    // Tránh việc lỗi runtime làm sập Service chạy ngầm của hệ thống
                    Console.WriteLine($"[BACKGROUND WORKER ERROR] Lỗi khi dọn dẹp đơn quá hạn: {ex.Message}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}