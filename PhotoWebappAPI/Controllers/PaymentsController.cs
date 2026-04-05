using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data; // ĐẢM BẢO ĐÚNG NAMESPACE CỦA BẠN
using PhotoWebappAPI.Models; // ĐẢM BẢO ĐÚNG NAMESPACE CỦA BẠN
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PhotoWebappAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public PaymentsController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public class PaymentRequestDto
        {
            public int BookingId { get; set; }
            public double Amount { get; set; }
        }

        [HttpPost("create-url")]
        public IActionResult CreatePaymentUrl([FromBody] PaymentRequestDto dto)
        {
            string vnp_Returnurl = _configuration["VnPay:ReturnUrl"]?.Trim();
            string vnp_Url = _configuration["VnPay:BaseUrl"]?.Trim();
            string vnp_TmnCode = _configuration["VnPay:TmnCode"]?.Trim();
            string vnp_HashSecret = _configuration["VnPay:HashSecret"]?.Trim();

            // 🌟 1. Ép kiểu Amount về long (số nguyên) để tránh lỗi số thập phân của double
            long amount = (long)(dto.Amount * 100);

            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            vnp_Params.Add("vnp_Amount", amount.ToString());
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_IpAddr", "127.0.0.1");
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", "Thanh toan don hang " + dto.BookingId);
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_TxnRef", dto.BookingId.ToString() + "_" + DateTime.Now.Ticks.ToString());
            vnp_Params.Add("vnp_Version", "2.1.0");

            // 🌟 2. Chuẩn hóa thuật toán mã hóa URL theo đúng Sample Code của VNPay
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string queryString = data.ToString();

            // Xóa dấu '&' thừa ở cuối cùng
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, queryString);
            string paymentUrl = $"{vnp_Url}?{queryString}&vnp_SecureHash={vnp_SecureHash}";

            return Ok(new { url = paymentUrl });
        }

        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentReturn()
        {
            var vnpayData = Request.Query;
            var vnp_SecureHash = vnpayData["vnp_SecureHash"];
            var vnp_HashSecret = _configuration["VnPay:HashSecret"]?.Trim();

            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value) && kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    vnp_Params.Add(kv.Key, kv.Value);
                }
            }

            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            var checkSignature = HmacSHA512(vnp_HashSecret, queryString);

            if (checkSignature.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase))
            {
                var responseCode = vnpayData["vnp_ResponseCode"].ToString();
                var txnRef = vnpayData["vnp_TxnRef"].ToString();
                var amountString = vnpayData["vnp_Amount"].ToString();

                int bookingId = int.Parse(txnRef.Split('_')[0]);

                if (responseCode == "00")
                {
                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        TransactionNo = vnpayData["vnp_TransactionNo"],
                        OrderInfo = vnpayData["vnp_OrderInfo"],
                        Amount = double.Parse(amountString) / 100,
                        PaymentMethod = "VNPay",
                        Status = "Success",
                        PaymentDate = DateTime.Now
                    };
                    _context.Payments.Add(payment);

                    var booking = await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        booking.Status = "Paid";
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = "Thanh toán thành công!", bookingId = bookingId });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Thanh toán thất bại hoặc đã bị hủy." });
                }
            }
            else
            {
                return BadRequest(new { success = false, message = "Lỗi xác thực chữ ký VNPay" });
            }
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        // 🌟 3. Thuật toán so xếp (Sorting) chuẩn 100% theo SDK VNPay
        private class VnPayCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var vnpCompare = CompareInfo.GetCompareInfo("en-US");
                return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
            }
        }
    }
}