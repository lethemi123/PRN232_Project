using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectPRN232.DTO;
using ProjectPRN232.Models;              // Person
using ProjectPRN232.Services;            // ResetTokenService
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectPRN232.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;
        private readonly IConfiguration _config;
        private readonly ResetTokenService _resetTokenService;

        public AuthController(Prn212AssignmentContext context, IConfiguration config, ResetTokenService resetTokenService)
        {
            _context = context;
            _config = config;
            _resetTokenService = resetTokenService;
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLower();
        private static string NormalizeUserName(string userName) => userName.Trim();

        // =========================
        // REGISTER
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var email = NormalizeEmail(request.Email);
            var userName = NormalizeUserName(request.UserName);

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Thiếu thông tin bắt buộc." });
            
            var emailExists = await _context.People.AnyAsync(p => p.Email.ToLower() == email);
            if (emailExists) return BadRequest(new { message = "Email đã được sử dụng." });

            var user = new Person
            {
                UserName = userName,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Fname = request.FName,
                Lname = request.LName,
                RoleAccount = false, // default user
                Balance = 0
            };

            _context.People.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công." });
        }

        // =========================
        // LOGIN
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var userName = NormalizeUserName(request.UserName);

            var user = await _context.People.FirstOrDefaultAsync(x => x.UserName == userName);
            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            // BCrypt verify
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new { user.PersonId, user.UserName, user.Email, user.RoleAccount, user.Balance }
            });
        }

        // =========================
        // GOOGLE LOGIN (tối giản)
        // =========================
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _context.People.FirstOrDefaultAsync(p => p.Email.ToLower() == email);

            if (user == null)
            {
                // tạo user mới
                user = new Person
                {
                    Email = email,
                    UserName = email, // cho đơn giản: dùng email làm username
                    Fname = request.FName,
                    Lname = request.LName,
                    RoleAccount = false,
                    Balance = 0,

                    // nếu vẫn muốn có password thì hash random
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
                };

                _context.People.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new { user.PersonId, user.UserName, user.Email, user.RoleAccount, user.Balance }
            });
        }

        // =========================
        // FORGOT PASSWORD (email -> token)
        // =========================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var email = NormalizeEmail(request.Email);

            var user = await _context.People.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

            // Không lộ email tồn tại hay không
            if (user == null)
            {
                return Ok(new ForgotPasswordResponse
                {
                    Message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu."
                });
            }

            var token = _resetTokenService.CreateToken(email, 15);

            // Demo: trả link để test (sau này thay bằng gửi mail)
            var debugLink = $"https://your-frontend.com/reset-password?token={Uri.EscapeDataString(token)}";

            return Ok(new ForgotPasswordResponse
            {
                Message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu.",
                DebugLink = debugLink
            });
        }

        // =========================
        // RESET PASSWORD (token + newPassword)
        // =========================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu tối thiểu 6 ký tự." });

            if (!_resetTokenService.TryReadToken(request.Token, out var emailFromToken))
                return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

            var email = NormalizeEmail(emailFromToken);

            var user = await _context.People.FirstOrDefaultAsync(x => x.Email.ToLower() == email);
            if (user == null)
                return BadRequest(new { message = "Yêu cầu không hợp lệ." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }

        // =========================
        // JWT helper
        // =========================
        private string GenerateJwtToken(Person user)
        {
            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("Missing Jwt:Key in appsettings.json");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // RoleAccount bool -> role string
            var role = user.RoleAccount ? "Admin" : "User";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.PersonId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
