using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoWebappAPI.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với đơn đặt lịch
        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        public string TransactionNo { get; set; } // Mã giao dịch trả về từ VNPay (vnp_TransactionNo)
        public string OrderInfo { get; set; } // Nội dung thanh toán
        public double Amount { get; set; } // Số tiền cọc
        public string PaymentMethod { get; set; } = "VNPay";

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Trạng thái giao dịch: "Success" (Thành công) hoặc "Failed" (Thất bại)
        public string Status { get; set; }
    }
}
