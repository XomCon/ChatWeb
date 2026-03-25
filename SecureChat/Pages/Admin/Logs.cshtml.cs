using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Admin
{
    public class LogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LogsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; } = string.Empty;

        public List<LogVm> Logs { get; set; } = new();

        public class LogVm
        {
            public int Id { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (role != "Admin")
            {
                return RedirectToPage("/Index");
            }

            var query = _context.AuditLogs.Include(l => l.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.Trim().ToLower();
                query = query.Where(l =>
                    l.Action.ToLower().Contains(keyword) ||
                    (l.Description != null && l.Description.ToLower().Contains(keyword)) ||
                    (l.User != null && l.User.Email != null && l.User.Email.ToLower().Contains(keyword)));
            }

            Logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(300)
                .Select(l => new LogVm
                {
                    Id = l.Id,
                    UserEmail = l.User != null ? (l.User.Email ?? "Không rõ") : "Không rõ",
                    Action = l.Action,
                    Description = l.Description ?? "",
                    IpAddress = l.IpAddress ?? "",
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Page();
        }
    }
}