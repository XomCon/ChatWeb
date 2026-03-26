using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;

        public LoginModel(ApplicationDbContext context, OtpService otpService, EmailService emailService)
        {
            _context = context;
            _otpService = otpService;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]!.ToString()!;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ.";
                return Page();
            }

            var email = Input.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                ErrorMessage = "Email hoặc mật khẩu không đúng.";
                return Page();
            }

            if (!user.IsActive)
            {
                ErrorMessage = "Tài khoản đã bị khóa.";
                return Page();
            }

            if (user.FailedLoginCount >= 5)
            {
                ErrorMessage = "Tài khoản đã bị khóa do nhập sai quá 5 lần.";
                return Page();
            }

            bool isPasswordValid = false;

            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash);
            }
            catch
            {
                ErrorMessage = "Dữ liệu mật khẩu không hợp lệ.";
                return Page();
            }

            if (!isPasswordValid)
            {
                user.FailedLoginCount += 1;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                ErrorMessage = "Email hoặc mật khẩu không đúng.";
                return Page();
            }

            var cookieDeviceId = Request.Cookies["device_id"];

            if (!string.IsNullOrEmpty(cookieDeviceId) &&
                !string.IsNullOrEmpty(user.CurrentDeviceId) &&
                cookieDeviceId == user.CurrentDeviceId)
            {
                user.FailedLoginCount = 0;
                user.IsOnline = true;
                user.LastLoginAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("FullName", user.FullName ?? "");
                HttpContext.Session.SetString("Email", user.Email ?? "");
                HttpContext.Session.SetString("Role", user.Role ?? "User");

                return RedirectToPage("/Index");
            }

            var otpCode = _otpService.GenerateOtp();

            var oldOtps = await _context.OtpVerifications
                .Where(x => x.UserId == user.Id && x.Purpose == "Login" && !x.IsUsed)
                .ToListAsync();

            if (oldOtps.Any())
            {
                _context.OtpVerifications.RemoveRange(oldOtps);
                await _context.SaveChangesAsync();
            }

            var otp = new Models.OtpVerification
            {
                UserId = user.Id,
                OtpCode = otpCode,
                Purpose = "Login",
                ExpiredAt = _otpService.GetExpiredTime(5),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.OtpVerifications.Add(otp);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(
                    user.Email!,
                    user.FullName ?? "Người dùng",
                    otpCode,
                    "Đăng nhập");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không gửi được OTP Gmail: " + ex.Message;
                return Page();
            }

            HttpContext.Session.SetString("PendingLoginUserId", user.Id);

            return RedirectToPage("/Account/VerifyLoginOtp");
        }
    }
}