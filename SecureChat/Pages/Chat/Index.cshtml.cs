using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Chat
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ChatFriendVm> Friends { get; set; } = new();

        public class ChatFriendVm
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsOnline { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            Friends = await _context.Friends
                .Where(f => f.UserId == currentUserId)
                .Select(f => new ChatFriendVm
                {
                    UserId = f.FriendUserId,
                    FullName = f.FriendUser != null ? (f.FriendUser.FullName ?? "Người dùng") : "Người dùng",
                    Email = f.FriendUser != null ? (f.FriendUser.Email ?? "") : "",
                    IsOnline = f.FriendUser != null && f.FriendUser.IsOnline
                })
                .OrderBy(f => f.FullName)
                .ToListAsync();

            return Page();
        }
    }
}