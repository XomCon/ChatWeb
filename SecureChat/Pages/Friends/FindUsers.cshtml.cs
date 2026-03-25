using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Services;

namespace SecureChat.Pages.Friends
{
    public class FindUsersModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public FindUsersModel(ApplicationDbContext context, AuditLogService auditLogService)
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

        // ===== Data =====
        public List<UserVm> Users { get; set; } = new();

        // ===== ViewModel =====
        public class UserVm
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public bool IsOnline { get; set; }
            public bool IsFriend { get; set; }
            public bool IsPendingSent { get; set; }
            public bool IsPendingReceived { get; set; }
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var query = _context.Users
                .Where(u => u.Id != currentUserId && u.IsActive);

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                var keyword = Keyword.Trim().ToLower();

                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(keyword)) ||
                    (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)));
            }

            var users = await query
                .OrderBy(u => u.FullName)
                .Take(50)
                .ToListAsync();

            var friendIds = await _context.Friends
                .Where(f => f.UserId == currentUserId)
                .Select(f => f.FriendUserId)
                .ToListAsync();

            var pendingSentIds = await _context.FriendRequests
                .Where(fr => fr.SenderId == currentUserId && fr.Status == "Pending")
                .Select(fr => fr.ReceiverId)
                .ToListAsync();

            var pendingReceivedIds = await _context.FriendRequests
                .Where(fr => fr.ReceiverId == currentUserId && fr.Status == "Pending")
                .Select(fr => fr.SenderId)
                .ToListAsync();

            Users = users.Select(u => new UserVm
            {
                Id = u.Id,
                FullName = u.FullName ?? "Người dùng",
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber ?? "",
                IsOnline = u.IsOnline,
                IsFriend = friendIds.Contains(u.Id),
                IsPendingSent = pendingSentIds.Contains(u.Id),
                IsPendingReceived = pendingReceivedIds.Contains(u.Id)
            }).ToList();

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

        // ===== SEND FRIEND REQUEST =====
        public async Task<IActionResult> OnPostSendRequestAsync(string receiverId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (string.IsNullOrEmpty(receiverId) || receiverId == currentUserId)
            {
                TempData["ErrorMessage"] = "Không thể gửi lời mời kết bạn.";
                return RedirectToPage(new { Keyword });
            }

            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == receiverId && u.IsActive);

            if (receiver == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToPage(new { Keyword });
            }

            var isFriend = await _context.Friends
                .AnyAsync(f => f.UserId == currentUserId && f.FriendUserId == receiverId);

            if (isFriend)
            {
                TempData["ErrorMessage"] = "Hai bạn đã là bạn bè.";
                return RedirectToPage(new { Keyword });
            }

            var alreadyPending = await _context.FriendRequests
                .AnyAsync(fr =>
                    ((fr.SenderId == currentUserId && fr.ReceiverId == receiverId) ||
                     (fr.SenderId == receiverId && fr.ReceiverId == currentUserId))
                    && fr.Status == "Pending");

            if (alreadyPending)
            {
                TempData["ErrorMessage"] = "Đã tồn tại lời mời kết bạn đang chờ xử lý.";
                return RedirectToPage(new { Keyword });
            }

            var request = new Models.FriendRequest
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "SendFriendRequest",
                $"Gửi lời mời kết bạn tới userId={receiverId}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã gửi lời mời kết bạn.";
            return RedirectToPage(new { Keyword });
        }
    }
}