using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.Models;

namespace PhotoWebappAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🌟 Đã thêm tham số 'userId' vào đây
        public async Task SendMessage(string bookingId, string userId, string senderName, string message, string imageUrl = "")
        {
            // Lưu thẳng ID lấy từ React vào Database, không cần tìm kiếm nữa
            var newMessage = new Message
            {
                BookingId = int.Parse(bookingId),
                SenderId = userId,
                Content = message,
                ImageUrl = imageUrl,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync(); // Sẽ không còn bị lỗi Foreign Key nữa!

            await Clients.Group(bookingId).SendAsync("ReceiveMessage", senderName, message, imageUrl);
        }

        public async Task JoinChatRoom(string bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, bookingId);
        }
    }
}