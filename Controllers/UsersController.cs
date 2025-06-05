using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Vidly.Data;
using Vidly.Models;

namespace Vidly.Controllers
{
    public class UsersController : Controller
    {
        private readonly VidlyContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(VidlyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Users/Login View
        public IActionResult Login()
        {
            return View("~/Views/Auth/Login.cshtml");
        }

        // POST: Users/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    // Store user ID and username in session first
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("Username", user.Username);

                    // Then, proceed with JWT generation and return JSON as before
                    var jwtSettings = _configuration.GetSection("Jwt");
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)); // Added null-forgiving operator for safety
                    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.Now.AddHours(3),
                        signingCredentials: credentials
                    );

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                    return Json(new
                    {
                        success = true, // Indicate success
                        token = tokenString,
                        expiration = token.ValidTo,
                        username = user.Username
                        // You might want to redirect from client-side based on this success response
                    });
                }
                else
                {
                    return Json(new { error = "Invalid username or password" });
                }
            }
            return Json(new { error = "Invalid model state" });
        }

        // GET: Users/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Json(new { success = true });
        }

        // GET: Users/Signup View
        public IActionResult Signup()
        {
            return View("~/Views/Auth/Signup.cshtml");
        }

        // POST: Users/Signup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup([Bind("Id,Username,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                // Hash password before storing
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }
            return View("~/Views/Auth/Signup.cshtml", user);
        }
    }
}
