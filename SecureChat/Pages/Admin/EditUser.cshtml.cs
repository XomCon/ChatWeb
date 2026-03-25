using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Admin
{
    public class EditUserModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public EditUserModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            public string FullName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = "User";

            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null) return RedirectToPage("/Admin/Users");

            Input = new InputModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Role = user.Role ?? "User",
                IsActive = user.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ.";
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null) return RedirectToPage("/Admin/Users");

            var oldRole = user.Role;
            var oldActive = user.IsActive;

            user.FullName = Input.FullName.Trim();
            user.Email = Input.Email.Trim().ToLower();
            user.PhoneNumber = Input.PhoneNumber.Trim();
            user.Role = Input.Role;
            user.IsActive = Input.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (!user.IsActive)
            {
                user.IsOnline = false;
            }

            await _context.SaveChangesAsync();

            var adminId = HttpContext.Session.GetString("UserId");

            await _auditLogService.WriteLogAsync(
                adminId,
                "EditUser",
                $"Admin sửa userId={user.Id}, Role: {oldRole}->{user.Role}, Active: {oldActive}->{user.IsActive}",
                HttpContext);

            SuccessMessage = "Cập nhật tài khoản thành công.";
            return Page();
        }

        private IActionResult? CheckAdmin()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            if (role != "Admin")
                return RedirectToPage("/Index");

            return null;
        }
    }
}