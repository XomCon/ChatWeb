using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Admin
{
    public class IndexModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public IndexModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ===== Bind Properties =====
        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; } = string.Empty;

        // ===== Messages =====
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        // ===== Statistics =====
        public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int ActiveUsers { get; set; }

        // ===== Data =====
        public List<UserVm> Users { get; set; } = new();

        // ===== ViewModel =====
        public class UserVm
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
            public bool IsActive { get; set; }
            public bool IsOnline { get; set; }
            public bool IsVerified { get; set; }
            public int FailedLoginCount { get; set; }
            public DateTime? LastLoginAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var authResult = CheckAdminAccess();
            if (authResult != null)
            {
                return authResult;
            }

            await LoadStatsAsync();

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.Trim().ToLower();

                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(keyword)) ||
                    (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)) ||
                    (u.Role != null && u.Role.ToLower().Contains(keyword)));
            }

            Users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserVm
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "Người dùng",
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber ?? "",
                    Role = u.Role ?? "User",
                    IsActive = u.IsActive,
                    IsOnline = u.IsOnline,
                    IsVerified = u.IsVerified,
                    FailedLoginCount = u.FailedLoginCount,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

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

        // ===== TOGGLE ACTIVE =====
        public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
        {
            var authResult = CheckAdminAccess();
            if (authResult != null)
            {
                return authResult;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Không xác định được tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");

            if (currentUserId == userId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToPage(new { Keyword });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (!user.IsActive)
            {
                user.IsOnline = false;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsActive
                ? "Đã mở khóa tài khoản."
                : "Đã khóa tài khoản.";

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "ToggleUserActive",
                $"Admin thay đổi trạng thái hoạt động của userId={userId} thành IsActive={user.IsActive}",
                HttpContext);

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "ToggleUserRole",
                $"Admin đổi quyền userId={userId} thành Role={user.Role}",
                HttpContext);

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "ResetFailedLogin",
                $"Admin reset số lần đăng nhập sai cho userId={userId}",
                HttpContext);

            return RedirectToPage(new { Keyword });
        }

        // ===== TOGGLE ROLE =====
        public async Task<IActionResult> OnPostToggleRoleAsync(string userId)
        {
            var authResult = CheckAdminAccess();
            if (authResult != null)
            {
                return authResult;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Không xác định được tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");

            if (currentUserId == userId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự đổi quyền của chính mình.";
                return RedirectToPage(new { Keyword });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            user.Role = user.Role == "Admin" ? "User" : "Admin";
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã đổi quyền tài khoản thành {user.Role}.";
            return RedirectToPage(new { Keyword });
        }

        // ===== RESET FAILED LOGIN =====
        public async Task<IActionResult> OnPostResetFailedLoginAsync(string userId)
        {
            var authResult = CheckAdminAccess();
            if (authResult != null)
            {
                return authResult;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Không xác định được tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToPage(new { Keyword });
            }

            user.FailedLoginCount = 0;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã reset số lần đăng nhập sai.";
            return RedirectToPage(new { Keyword });
        }

        // ===== PRIVATE METHODS =====
        private IActionResult? CheckAdminAccess()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (role != "Admin")
            {
                return RedirectToPage("/Index");
            }

            return null;
        }

        private async Task LoadStatsAsync()
        {
            TotalUsers = await _context.Users.CountAsync();
            OnlineUsers = await _context.Users.CountAsync(u => u.IsOnline);
            TotalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin");
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
        }
    }
}