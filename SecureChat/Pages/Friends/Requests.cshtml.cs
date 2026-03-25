using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Pages.Friends
{
    public class RequestsModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public RequestsModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ===== Properties =====
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public List<RequestVm> IncomingRequests { get; set; } = new();
        public List<RequestVm> SentRequests { get; set; } = new();

        // ===== ViewModel =====
        public class RequestVm
        {
            public int Id { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Incoming Requests
            IncomingRequests = await _context.FriendRequests
                .Where(fr => fr.ReceiverId == currentUserId && fr.Status == "Pending")
                .OrderByDescending(fr => fr.CreatedAt)
                .Select(fr => new RequestVm
                {
                    Id = fr.Id,
                    UserId = fr.SenderId,
                    FullName = fr.Sender != null ? (fr.Sender.FullName ?? "Người dùng") : "Người dùng",
                    Email = fr.Sender != null ? (fr.Sender.Email ?? "") : "",
                    Status = fr.Status,
                    CreatedAt = fr.CreatedAt
                })
                .ToListAsync();

            // Sent Requests
            SentRequests = await _context.FriendRequests
                .Where(fr => fr.SenderId == currentUserId && fr.Status == "Pending")
                .OrderByDescending(fr => fr.CreatedAt)
                .Select(fr => new RequestVm
                {
                    Id = fr.Id,
                    UserId = fr.ReceiverId,
                    FullName = fr.Receiver != null ? (fr.Receiver.FullName ?? "Người dùng") : "Người dùng",
                    Email = fr.Receiver != null ? (fr.Receiver.Email ?? "") : "",
                    Status = fr.Status,
                    CreatedAt = fr.CreatedAt
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

        // ===== ACCEPT =====
        public async Task<IActionResult> OnPostAcceptAsync(int requestId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(fr =>
                    fr.Id == requestId &&
                    fr.ReceiverId == currentUserId &&
                    fr.Status == "Pending");

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lời mời kết bạn hợp lệ.";
                return RedirectToPage();
            }

            var alreadyFriend = await _context.Friends
                .AnyAsync(f =>
                    f.UserId == currentUserId &&
                    f.FriendUserId == request.SenderId);

            if (!alreadyFriend)
            {
                _context.Friends.Add(new Friend
                {
                    UserId = currentUserId,
                    FriendUserId = request.SenderId,
                    CreatedAt = DateTime.Now
                });

                _context.Friends.Add(new Friend
                {
                    UserId = request.SenderId,
                    FriendUserId = currentUserId,
                    CreatedAt = DateTime.Now
                });
            }

            request.Status = "Accepted";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã chấp nhận lời mời kết bạn.";
            return RedirectToPage();
        }

        // ===== REJECT =====
        public async Task<IActionResult> OnPostRejectAsync(int requestId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(fr =>
                    fr.Id == requestId &&
                    fr.ReceiverId == currentUserId &&
                    fr.Status == "Pending");

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lời mời kết bạn hợp lệ.";
                return RedirectToPage();
            }

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã từ chối lời mời kết bạn.";
            return RedirectToPage();
        }

        // ===== CANCEL =====
        public async Task<IActionResult> OnPostCancelAsync(int requestId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(fr =>
                    fr.Id == requestId &&
                    fr.SenderId == currentUserId &&
                    fr.Status == "Pending");

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lời mời để hủy.";
                return RedirectToPage();
            }

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "AcceptFriendRequest",
                $"Chấp nhận lời mời kết bạn từ userId={request.SenderId}",
                HttpContext);

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "AcceptFriendRequest",
                $"Chấp nhận lời mời kết bạn từ userId={request.SenderId}",
                HttpContext);

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "CancelFriendRequest",
                $"Hủy lời mời kết bạn tới userId={request.ReceiverId}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã hủy lời mời kết bạn.";
            return RedirectToPage();
        }
    }
}