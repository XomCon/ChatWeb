using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public ForgotPasswordModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8 ký tự trở lên.")]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu mới không khớp.")]
            [Display(Name = "Xác nhận mật khẩu mới")]
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]!.ToString()!;
            }

            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]!.ToString()!;
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

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                ErrorMessage = "Không tìm thấy tài khoản với email này.";
                return Page();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            user.FailedLoginCount = 0;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                user.Id,
                "ForgotPassword",
                $"Người dùng đặt lại mật khẩu qua chức năng quên mật khẩu: {user.Email}",
                HttpContext);

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay.";
            return RedirectToPage("/Account/Login");
        }
    }
}