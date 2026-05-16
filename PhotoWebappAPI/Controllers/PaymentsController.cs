using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoWebappAPI.Data;
using PhotoWebappAPI.Models;
using System.Globalization;
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

        public PaymentsController(
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public class PaymentRequestDto
        {
            public int BookingId { get; set; }
        }

        [HttpPost("create-url")]
        public async Task<IActionResult> CreatePaymentUrl(
            [FromBody] PaymentRequestDto dto)
        {
            try
            {
                string vnp_Returnurl =
                    _configuration["VnPay:ReturnUrl"]?.Trim();

                string vnp_Url =
                    _configuration["VnPay:BaseUrl"]?.Trim();

                string vnp_TmnCode =
                    _configuration["VnPay:TmnCode"]?.Trim();

                string vnp_HashSecret =
                    _configuration["VnPay:HashSecret"]?.Trim();

                string frontendUrl =
                    _configuration["FrontendUrl"]?.Trim();

                if (string.IsNullOrEmpty(vnp_Returnurl) ||
                    string.IsNullOrEmpty(vnp_Url) ||
                    string.IsNullOrEmpty(vnp_TmnCode) ||
                    string.IsNullOrEmpty(vnp_HashSecret))
                {
                    return BadRequest("VNPay configuration missing");
                }

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(x => x.Id == dto.BookingId);

                if (booking == null)
                {
                    return NotFound("Booking not found");
                }

                if (booking.Status == "Paid")
                {
                    return BadRequest("Booking already paid");
                }

                decimal bookingAmount = Convert.ToDecimal(booking.TotalPrice);

                long amount = (long)(bookingAmount * 100);

                TimeZoneInfo vnTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "SE Asia Standard Time"
                    );

                DateTime vnTime =
                    TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow,
                        vnTimeZone
                    );

                string txnRef =
                    $"{booking.Id}_{DateTime.Now.Ticks}";

                string ipAddress =
                    HttpContext.Connection.RemoteIpAddress?
                    .MapToIPv4()
                    .ToString() ?? "127.0.0.1";

                var vnp_Params =
                    new SortedList<string, string>(
                        new VnPayCompare()
                    );

                vnp_Params.Add("vnp_Version", "2.1.0");
                vnp_Params.Add("vnp_Command", "pay");
                vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
                vnp_Params.Add("vnp_Amount", amount.ToString());
                vnp_Params.Add("vnp_CreateDate",
                    vnTime.ToString("yyyyMMddHHmmss"));
                vnp_Params.Add("vnp_CurrCode", "VND");
                vnp_Params.Add("vnp_IpAddr", ipAddress);
                vnp_Params.Add("vnp_Locale", "vn");
                vnp_Params.Add("vnp_OrderInfo",
                    $"Thanh toan don hang {booking.Id}");
                vnp_Params.Add("vnp_OrderType", "other");
                vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
                vnp_Params.Add("vnp_TxnRef", txnRef);

                // Expire after 15 minutes
                vnp_Params.Add(
                    "vnp_ExpireDate",
                    vnTime.AddMinutes(15)
                        .ToString("yyyyMMddHHmmss")
                );

                StringBuilder hashData = new StringBuilder();
                StringBuilder query = new StringBuilder();

                foreach (KeyValuePair<string, string> kv in vnp_Params)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        // HASH DATA KHÔNG ENCODE
                        hashData.Append(
                            kv.Key + "=" + kv.Value + "&"
                        );

                        // QUERY URL PHẢI ENCODE
                        query.Append(
                            Uri.EscapeDataString(kv.Key) + "=" +
                            Uri.EscapeDataString(kv.Value) + "&"
                        );
                    }
                }

                string queryUrl = query.ToString();
                string hashDataString = hashData.ToString();

                if (queryUrl.Length > 0)
                {
                    queryUrl =
                        queryUrl.Remove(queryUrl.Length - 1, 1);
                }

                if (hashDataString.Length > 0)
                {
                    hashDataString =
                        hashDataString.Remove(
                            hashDataString.Length - 1,
                            1
                        );
                }

                string vnp_SecureHash =
                    HmacSHA512(
                        vnp_HashSecret,
                        hashDataString
                    );

                string paymentUrl =
                    $"{vnp_Url}?{queryUrl}" +
                    $"&vnp_SecureHash={vnp_SecureHash}";

                // Lưu payment pending trước
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    TransactionNo = txnRef,
                    OrderInfo = $"Thanh toan don hang {booking.Id}",
                    Amount = bookingAmount,
                    PaymentMethod = "VNPay",
                    Status = "Pending",
                    PaymentDate = vnTime
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    url = paymentUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentReturn()
        {
            try
            {
                var vnpayData = Request.Query;

                string vnp_HashSecret =
                    _configuration["VnPay:HashSecret"]?.Trim();

                string frontendUrl =
                    _configuration["FrontendUrl"]?.Trim();

                string vnp_SecureHash =
                    vnpayData["vnp_SecureHash"];

                var vnp_Params =
                    new SortedList<string, string>(
                        new VnPayCompare()
                    );

                foreach (var kv in vnpayData)
                {
                    if (!string.IsNullOrEmpty(kv.Value)
                        && kv.Key.StartsWith("vnp_")
                        && kv.Key != "vnp_SecureHash"
                        && kv.Key != "vnp_SecureHashType")
                    {
                        vnp_Params.Add(kv.Key, kv.Value);
                    }
                }

                StringBuilder hashData =
                    new StringBuilder();

                foreach (KeyValuePair<string, string> kv in vnp_Params)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        // KHÔNG ENCODE HASH DATA
                        hashData.Append(
                            kv.Key + "=" + kv.Value + "&"
                        );
                    }
                }

                string hashDataString =
                    hashData.ToString();

                if (hashDataString.Length > 0)
                {
                    hashDataString =
                        hashDataString.Remove(
                            hashDataString.Length - 1,
                            1
                        );
                }

                string checkSignature =
                    HmacSHA512(
                        vnp_HashSecret,
                        hashDataString
                    );

                if (!checkSignature.Equals(
                    vnp_SecureHash,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    return Redirect(
                        $"{frontendUrl}/payment-result" +
                        $"?success=false" +
                        $"&message=invalid-signature"
                    );
                }

                string responseCode =
                    vnpayData["vnp_ResponseCode"];

                string txnRef =
                    vnpayData["vnp_TxnRef"];

                string amountString =
                    vnpayData["vnp_Amount"];

                string transactionNo =
                    vnpayData["vnp_TransactionNo"];

                int bookingId =
                    int.Parse(txnRef.Split('_')[0]);

                decimal amount =
                    decimal.Parse(amountString) / 100;

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(
                        p => p.TransactionNo == txnRef
                    );

                if (payment == null)
                {
                    return Redirect(
                        $"{frontendUrl}/payment-result" +
                        $"?success=false" +
                        $"&message=payment-not-found"
                    );
                }

                // Đã xử lý trước đó
                if (payment.Status == "Success")
                {
                    return Redirect(
                        $"{frontendUrl}/payment-result" +
                        $"?success=true" +
                        $"&bookingId={bookingId}"
                    );
                }

                if (responseCode == "00")
                {
                    payment.Status = "Success";
                    payment.TransactionNo = transactionNo;
                    payment.Amount = amount;
                    payment.PaymentDate = DateTime.Now;

                    var booking =
                        await _context.Bookings
                            .FindAsync(bookingId);

                    if (booking != null)
                    {
                        booking.Status = "Paid";
                    }

                    await _context.SaveChangesAsync();

                    return Redirect(
                        $"{frontendUrl}/payment-result" +
                        $"?success=true" +
                        $"&bookingId={bookingId}"
                    );
                }
                else
                {
                    payment.Status = "Failed";

                    await _context.SaveChangesAsync();

                    return Redirect(
                        $"{frontendUrl}/payment-result" +
                        $"?success=false"
                    );
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        private string HmacSHA512(
            string key,
            string inputData)
        {
            byte[] keyBytes =
                Encoding.UTF8.GetBytes(key);

            byte[] inputBytes =
                Encoding.UTF8.GetBytes(inputData);

            using (var hmac =
                   new HMACSHA512(keyBytes))
            {
                byte[] hashValue =
                    hmac.ComputeHash(inputBytes);

                StringBuilder hash =
                    new StringBuilder();

                foreach (var theByte in hashValue)
                {
                    hash.Append(
                        theByte.ToString("x2")
                    );
                }

                return hash.ToString();
            }
        }

        private class VnPayCompare : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                var compare =
                    CompareInfo.GetCompareInfo("en-US");

                return compare.Compare(
                    x,
                    y,
                    CompareOptions.Ordinal
                );
            }
        }
    }
}