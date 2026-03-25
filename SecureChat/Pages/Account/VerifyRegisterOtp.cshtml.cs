using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;

namespace SecureChat.Pages.Account
{
    public class VerifyRegisterOtpModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VerifyRegisterOtpModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int PendingId { get; set; }

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
            var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(x => x.Id == PendingId);
            if (pending == null)
            {
                return RedirectToPage("/Account/Register");
            }

            Email = pending.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(x => x.Id == PendingId);
            if (pending == null)
            {
                return RedirectToPage("/Account/Register");
            }

            Email = pending.Email;

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập OTP.";
                return Page();
            }

            if (pending.ExpiredAt < DateTime.Now)
            {
                ErrorMessage = "OTP đã hết hạn.";
                return Page();
            }

            if (pending.OtpCode != Input.OtpCode.Trim())
            {
                ErrorMessage = "OTP không chính xác.";
                return Page();
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                FullName = pending.FullName,
                Email = pending.Email,
                PhoneNumber = pending.PhoneNumber,
                PasswordHash = pending.PasswordHash,
                Role = "User",
                IsActive = true,
                FailedLoginCount = 0,
                CurrentDeviceId = null,
                IsOnline = false,
                IsVerified = true,
                LastLoginAt = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            _context.Users.Add(user);
            _context.PendingRegistrations.Remove(pending);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công. Bạn có thể đăng nhập.";
            return RedirectToPage("/Account/Login");
        }
    }
}