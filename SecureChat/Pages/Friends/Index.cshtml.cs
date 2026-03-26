using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Pages.Friends
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<FriendVm> Friends { get; set; } = new();
        public int PendingCount { get; set; }
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;


        public class FriendVm
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
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
                .Select(f => new FriendVm
                {
                    UserId = f.FriendUserId,
                    FullName = f.FriendUser != null ? (f.FriendUser.FullName ?? "Người dùng") : "Người dùng",
                    Email = f.FriendUser != null ? (f.FriendUser.Email ?? "") : "",
                    PhoneNumber = f.FriendUser != null ? (f.FriendUser.PhoneNumber ?? "") : "",
                    IsOnline = f.FriendUser != null && f.FriendUser.IsOnline
                })
                .OrderBy(f => f.FullName)
                .ToListAsync();

            PendingCount = await _context.FriendRequests
                .CountAsync(fr => fr.ReceiverId == currentUserId && fr.Status == "Pending");

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

        public async Task<IActionResult> OnPostRemoveAsync(string friendUserId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (string.IsNullOrEmpty(friendUserId))
            {
                TempData["ErrorMessage"] = "Không xác định được bạn bè cần xóa.";
                return RedirectToPage();
            }

            var relations = await _context.Friends
                .Where(f =>
                    (f.UserId == currentUserId && f.FriendUserId == friendUserId) ||
                    (f.UserId == friendUserId && f.FriendUserId == currentUserId))
                .ToListAsync();

            if (!relations.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy mối quan hệ bạn bè.";
                return RedirectToPage();
            }

            _context.Friends.RemoveRange(relations);

            var relatedRequests = await _context.FriendRequests
                .Where(fr =>
                    (fr.SenderId == currentUserId && fr.ReceiverId == friendUserId) ||
                    (fr.SenderId == friendUserId && fr.ReceiverId == currentUserId))
                .ToListAsync();

            if (relatedRequests.Any())
            {
                _context.FriendRequests.RemoveRange(relatedRequests);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa bạn bè thành công.";
            return RedirectToPage();
        }
    }
}