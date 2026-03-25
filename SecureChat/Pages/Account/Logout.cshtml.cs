using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Account
{
    public class LogoutModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public LogoutModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    user.IsOnline = false;
                    user.CurrentDeviceId = null;
                    user.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
            }

            await _auditLogService.WriteLogAsync(
                userId,
                "Logout",
                "Người dùng đăng xuất khỏi hệ thống",
                HttpContext);

            HttpContext.Session.Clear();
            Response.Cookies.Delete("device_id");

            return RedirectToPage("/Index");
        }
    }
}