using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Groups
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public List<GroupVm> Groups { get; set; } = new();

        public class GroupVm
        {
            public int ConversationId { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public int MemberCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            Groups = await _context.ConversationMembers
                .Where(cm => cm.UserId == currentUserId && cm.Conversation != null && cm.Conversation.Type == "Group")
                .Select(cm => new GroupVm
                {
                    ConversationId = cm.ConversationId,
                    GroupName = cm.Conversation != null ? (cm.Conversation.Name ?? "Nhóm chat") : "Nhóm chat",
                    MemberCount = cm.Conversation != null ? cm.Conversation.Members.Count : 0,
                    CreatedAt = cm.Conversation != null ? cm.Conversation.CreatedAt : DateTime.Now
                })
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

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
    }
}