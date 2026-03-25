using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureChat.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public ChangePasswordModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ===== Bind Property =====
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ===== Messages =====
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        // ===== Input Model =====
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu hiện tại")]
            public string CurrentPassword { get; set; } = string.Empty;

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

        // ===== GET =====
        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Messages
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]!.ToString()!;
            }

            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]!.ToString()!;
            }

            return Page();
        }

        // ===== POST =====
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ.";
                return Page();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            bool isCurrentPasswordValid = false;

            try
            {
                isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                    Input.CurrentPassword,
                    user.PasswordHash
                );
            }
            catch
            {
                ErrorMessage = "Dữ liệu mật khẩu hiện tại của tài khoản không hợp lệ.";
                return Page();
            }

            if (!isCurrentPasswordValid)
            {
                ErrorMessage = "Mật khẩu hiện tại không đúng.";
                return Page();
            }

            if (Input.CurrentPassword == Input.NewPassword)
            {
                ErrorMessage = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                return Page();
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            user.FailedLoginCount = 0;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                user.Id,
                "ChangePassword",
                $"Người dùng đổi mật khẩu: {user.Email}",
                HttpContext);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToPage("/Account/ChangePassword");
        }
    }
}