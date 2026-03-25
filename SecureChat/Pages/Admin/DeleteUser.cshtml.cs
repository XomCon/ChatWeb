using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Admin
{
    public class DeleteUserModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        public DeleteUserModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }
        [BindProperty(SupportsGet = true)]
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public async Task<IActionResult> OnGetAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null) return RedirectToPage("/Admin/Users");

            FullName = user.FullName ?? "";
            Email = user.Email ?? "";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var auth = CheckAdmin();
            if (auth != null) return auth;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null) return RedirectToPage("/Admin/Users");

            var adminId = HttpContext.Session.GetString("UserId");

            var friends = await _context.Friends
                .Where(f => f.UserId == user.Id || f.FriendUserId == user.Id)
                .ToListAsync();

            var requests = await _context.FriendRequests
                .Where(fr => fr.SenderId == user.Id || fr.ReceiverId == user.Id)
                .ToListAsync();

            var otpList = await _context.OtpVerifications
                .Where(o => o.UserId == user.Id)
                .ToListAsync();

            var members = await _context.ConversationMembers
                .Where(cm => cm.UserId == user.Id)
                .ToListAsync();

            var messages = await _context.Messages
                .Where(m => m.SenderId == user.Id)
                .ToListAsync();

            var auditLogs = await _context.AuditLogs
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            if (friends.Any()) _context.Friends.RemoveRange(friends);
            if (requests.Any()) _context.FriendRequests.RemoveRange(requests);
            if (otpList.Any()) _context.OtpVerifications.RemoveRange(otpList);
            if (members.Any()) _context.ConversationMembers.RemoveRange(members);
            if (messages.Any()) _context.Messages.RemoveRange(messages);
            if (auditLogs.Any()) _context.AuditLogs.RemoveRange(auditLogs);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                adminId,
                "DeleteUser",
                $"Admin xóa userId={user.Id}, email={user.Email}",
                HttpContext);

            return RedirectToPage("/Admin/Users");
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