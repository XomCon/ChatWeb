using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;

        public RegisterModel(ApplicationDbContext context, OtpService otpService, EmailService emailService)
        {
            _context = context;
            _otpService = otpService;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required] public string FullName { get; set; } = string.Empty;
            [Required, EmailAddress] public string Email { get; set; } = string.Empty;
            [Required] public string PhoneNumber { get; set; } = string.Empty;
            [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
            [Required, DataType(DataType.Password), Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ.";
                return Page();
            }

            var email = Input.Email.Trim().ToLower();
            var phone = Input.PhoneNumber.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "Vui lòng nhập Gmail.";
                return Page();
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                ErrorMessage = "Gmail không đúng định dạng.";
                return Page();
            }

            if (!email.EndsWith("@gmail.com"))
            {
                ErrorMessage = "Hệ thống chỉ hỗ trợ đăng ký bằng Gmail.";
                return Page();
            }

            if (email.Contains(" "))
            {
                ErrorMessage = "Gmail không hợp lệ.";
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
            {
                ErrorMessage = "Gmail đã tồn tại trong hệ thống.";
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == phone))
            {
                ErrorMessage = "Số điện thoại đã tồn tại.";
                return Page();
            }

            var oldPending = await _context.PendingRegistrations
                .Where(x => x.Email.ToLower() == email || x.PhoneNumber == phone)
                .ToListAsync();

            if (oldPending.Any())
            {
                _context.PendingRegistrations.RemoveRange(oldPending);
                await _context.SaveChangesAsync();
            }

            var otpCode = _otpService.GenerateOtp();

            var pending = new PendingRegistration
            {
                FullName = Input.FullName.Trim(),
                Email = email,
                PhoneNumber = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                OtpCode = otpCode,
                ExpiredAt = _otpService.GetExpiredTime(5),
                CreatedAt = DateTime.Now
            };

            _context.PendingRegistrations.Add(pending);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendOtpEmailAsync(
                    pending.Email,
                    pending.FullName,
                    otpCode,
                    "Đăng ký");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không gửi được OTP về Gmail: " + ex.Message;
                return Page();
            }

            return RedirectToPage("/Account/VerifyRegisterOtp", new { pendingId = pending.Id });
        }
    }
}