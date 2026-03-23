using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PhotoWebappAPI.DTOs.Auth;
using PhotoWebappAPI.Models;
using PhotoWebappAPI.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PhotoWebappAPI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // 1. Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return new AuthResponseDto { IsSuccess = false, Message = "Email đã được sử dụng!" };

            // 2. Tạo User mới
            var user = new AppUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { IsSuccess = false, Message = "Lỗi khi tạo tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description)) };

            // 3. Gán Role (Phân quyền)
            // Chỉ cho phép đăng ký Customer hoặc Photographer
            var roleToAssign = dto.Role == "Photographer" ? "Photographer" : "Customer";
            await _userManager.AddToRoleAsync(user, roleToAssign);

            return new AuthResponseDto { IsSuccess = true, Message = "Đăng ký thành công!" };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Email hoặc mật khẩu không đúng!" };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles.FirstOrDefault() ?? "Guest");

            return new AuthResponseDto
            {
                IsSuccess = true,
                Token = token,
                Role = roles.FirstOrDefault(),
                Message = "Đăng nhập thành công"
            };
        }

        // Hàm Private hỗ trợ tạo JWT Token
        private string GenerateJwtToken(AppUser user, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecurityKey"]!));

            // Đưa thông tin vào Payload của Token (Claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
