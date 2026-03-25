using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Account
{
    public class VerifyLoginOtpModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VerifyLoginOtpModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            public string OtpCode { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("PendingLoginUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Email = user.Email ?? "";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString("PendingLoginUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Email = user.Email ?? "";

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập OTP.";
                return Page();
            }

            var otp = await _context.OtpVerifications
                .Where(x => x.UserId == user.Id && x.Purpose == "Login" && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                ErrorMessage = "Không tìm thấy OTP đăng nhập.";
                return Page();
            }

            if (otp.ExpiredAt < DateTime.Now)
            {
                ErrorMessage = "OTP đã hết hạn.";
                return Page();
            }

            if (otp.OtpCode != Input.OtpCode.Trim())
            {
                ErrorMessage = "OTP không chính xác.";
                return Page();
            }

            otp.IsUsed = true;

            var currentDeviceId = Request.Cookies["device_id"];
            if (string.IsNullOrEmpty(currentDeviceId))
            {
                currentDeviceId = Guid.NewGuid().ToString();
            }

            user.CurrentDeviceId = currentDeviceId;
            user.FailedLoginCount = 0;
            user.IsOnline = true;
            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            Response.Cookies.Append("device_id", currentDeviceId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            HttpContext.Session.Remove("PendingLoginUserId");
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("Role", user.Role ?? "User");

            return RedirectToPage("/Index");
        }
    }
}