using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using ProjectPRN232.DTO;
using ProjectPRN232.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectPRN232.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;
        private readonly IConfiguration _config;
        public AuthController(Prn212AssignmentContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.People
                .FirstOrDefaultAsync(x => x.UserName == request.UserName
                                       && x.Password == request.Password);
            if (user == null) return Unauthorized("Sai tài khoản hoặc mật khẩu");

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new
                {
                    user.PersonId,
                    user.UserName,
                    user.RoleAccount,
                    user.Balance
                }
            });
        }

        private string GenerateJwtToken(Person user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var role = user.RoleAccount ? "Admin" : "Customer";

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.PersonId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role,role)
        };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                            double.Parse(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (await _context.People.AnyAsync(p => p.Email == request.Email))
            {
                return BadRequest("Email đã được sử dụng");
            }
            var user = new Person
            {
                UserName = request.UserName,
                Password = request.Password,
                Fname = request.FName,
                Lname = request.LName,
                Email = request.Email,
                RoleAccount = false,
                Balance = 0
            };
            _context.People.Add(user);
            await _context.SaveChangesAsync();
            var token = GenerateJwtToken(user);
            

            return Ok(new { message = "Register success", token});

        }
    }
}
