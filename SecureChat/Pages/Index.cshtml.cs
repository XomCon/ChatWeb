using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;

namespace SecureChat.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";

        public int FriendCount { get; set; }
        public int PendingCount { get; set; }
        public int GroupCount { get; set; }
        public int MessageCount { get; set; }

        public int TotalUsers { get; set; }
        public int TotalGroups { get; set; }
        public int OnlineUsers { get; set; }

        public async Task OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            IsLoggedIn = !string.IsNullOrEmpty(userId);

            TotalUsers = await _context.Users.CountAsync();
            TotalGroups = await _context.Conversations.CountAsync(c => c.Type == "Group");
            OnlineUsers = await _context.Users.CountAsync(u => u.IsOnline);

            if (!IsLoggedIn)
            {
                return;
            }

            FullName = HttpContext.Session.GetString("FullName") ?? "Người dùng";
            Email = HttpContext.Session.GetString("Email") ?? "";
            Role = HttpContext.Session.GetString("Role") ?? "User";
            IsAdmin = Role == "Admin";

            FriendCount = await _context.Friends.CountAsync(f => f.UserId == userId);
            PendingCount = await _context.FriendRequests.CountAsync(fr => fr.ReceiverId == userId && fr.Status == "Pending");
            GroupCount = await _context.ConversationMembers.CountAsync(cm => cm.UserId == userId && cm.Conversation.Type == "Group");
            MessageCount = await _context.Messages.CountAsync(m => m.SenderId == userId);
        }
    }
}
