using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string RoleFilter { get; set; } = string.Empty;

        public List<UserVm> Users { get; set; } = new();

        public class UserVm
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
            public bool IsActive { get; set; }
            public bool IsOnline { get; set; }
            public int FailedLoginCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(keyword)) ||
                    (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                query = query.Where(u => u.Role == RoleFilter);
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
                    FailedLoginCount = u.FailedLoginCount,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

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