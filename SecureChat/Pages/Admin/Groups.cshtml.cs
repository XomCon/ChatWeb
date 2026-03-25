using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Admin
{
    public class GroupsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GroupsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<GroupVm> Groups { get; set; } = new();

        public class GroupVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int MemberCount { get; set; }
            public int MessageCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            Groups = await _context.Conversations
                .Where(c => c.Type == "Group")
                .Select(c => new GroupVm
                {
                    Id = c.Id,
                    Name = c.Name ?? "Nhóm chat",
                    MemberCount = c.Members.Count,
                    MessageCount = c.Messages.Count,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(g => g.CreatedAt)
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