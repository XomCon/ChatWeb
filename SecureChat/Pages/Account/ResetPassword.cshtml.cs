using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureChat.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public ResetPasswordModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 ký tự trở lên.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
            public string Otp { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                return RedirectToPage("/Account/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                ErrorMessage = "Không tìm thấy người dùng với email này.";
                return Page();
            }

            var otpRecord = await _context.OtpVerifications
                .FirstOrDefaultAsync(o => o.UserId == user.Id && o.OtpCode == Input.Otp && o.Purpose == "PasswordReset" && o.ExpiredAt > DateTime.Now && !o.IsUsed);


            if (otpRecord == null)
            {
                ErrorMessage = "Mã OTP không hợp lệ, đã hết hạn hoặc đã được sử dụng.";
                return Page();
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password);
            user.UpdatedAt = DateTime.Now;
            user.FailedLoginCount = 0; // Reset locker

            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                user.Id,
                "ResetPassword",
                $"Người dùng đặt lại mật khẩu thành công: {user.Email}",
                HttpContext);

            SuccessMessage = "Mật khẩu của bạn đã được đặt lại thành công. Bạn hiện có thể đăng nhập.";
            return RedirectToPage("/Account/Login");
        }
    }
}
