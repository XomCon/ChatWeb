using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureChat.Pages.Groups
{
    public class CreateModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public CreateModel(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // ===== Bind Property =====
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ===== Data =====
        public string ErrorMessage { get; set; } = string.Empty;
        public List<FriendVm> Friends { get; set; } = new();

        // ===== ViewModels =====
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
            [StringLength(200, ErrorMessage = "Tên nhóm tối đa 200 ký tự.")]
            [Display(Name = "Tên nhóm")]
            public string GroupName { get; set; } = string.Empty;

            [Display(Name = "Thành viên")]
            public List<string> SelectedFriendIds { get; set; } = new();
        }

        public class FriendVm
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var result = await LoadFriendsAsync();

            if (result != null)
            {
                return result;
            }

            return Page();
        }

        // ===== POST =====
        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var loadResult = await LoadFriendsAsync();

            if (loadResult != null)
            {
                return loadResult;
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ.";
                return Page();
            }

            var validFriendIds = await _context.Friends
                .Where(f => f.UserId == currentUserId)
                .Select(f => f.FriendUserId)
                .ToListAsync();

            var selectedFriendIds = Input.SelectedFriendIds
                .Where(id => validFriendIds.Contains(id))
                .Distinct()
                .ToList();

            var conversation = new Conversation
            {
                Type = "Group",
                Name = Input.GroupName.Trim(),
                CreatedBy = currentUserId,
                CreatedAt = DateTime.Now
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            _context.ConversationMembers.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = currentUserId,
                Role = "GroupAdmin",
                JoinedAt = DateTime.Now
            });

            foreach (var friendId in selectedFriendIds)
            {
                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = friendId,
                    Role = "Member",
                    JoinedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tạo nhóm chat thành công.";

            await _auditLogService.WriteLogAsync(
                currentUserId,
                "CreateGroup",
                $"Tạo nhóm chat: {conversation.Name} (ConversationId={conversation.Id})",
                HttpContext);

            return RedirectToPage("/Groups/Chat", new { conversationId = conversation.Id });
        }

        // ===== PRIVATE METHODS =====
        private async Task<IActionResult?> LoadFriendsAsync()
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
                    FullName = f.FriendUser != null
                        ? (f.FriendUser.FullName ?? "Người dùng")
                        : "Người dùng",
                    Email = f.FriendUser != null ? (f.FriendUser.Email ?? "") : ""
                })
                .OrderBy(f => f.FullName)
                .ToListAsync();

            return null;
        }
    }
}