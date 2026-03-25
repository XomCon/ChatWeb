using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Pages.Groups
{
    public class ChatModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly EncryptionService _encryptionService;

        public ChatModel(
            ApplicationDbContext context,
            AuditLogService auditLogService,
            EncryptionService encryptionService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _encryptionService = encryptionService;
        }

        [BindProperty(SupportsGet = true)]
        public int ConversationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = string.Empty;

        [BindProperty]
        public List<string> SelectedFriendIds { get; set; } = new();

        public string GroupName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public bool IsGroupAdmin { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;

        public List<MemberVm> Members { get; set; } = new();
        public List<FriendVm> AvailableFriends { get; set; } = new();
        public List<MessageVm> Messages { get; set; } = new();

        public class MemberVm
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsCurrentUser { get; set; }
        }

        public class FriendVm
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class MessageVm
        {
            public int Id { get; set; }
            public string SenderId { get; set; } = string.Empty;
            public string SenderName { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime SentAt { get; set; }
            public bool IsMine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            var unread = await _context.Messages
                .Where(m => m.ConversationId == ConversationId
                         && m.SenderId != CurrentUserId
                         && !m.IsSeen)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var message in unread)
                {
                    message.IsSeen = true;
                }

                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var keyword = Search.Trim().ToLower();
                Messages = Messages
                    .Where(m =>
                        (!string.IsNullOrWhiteSpace(m.Content) && m.Content.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(m.SenderName) && m.SenderName.ToLower().Contains(keyword)))
                    .ToList();
            }

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

        public async Task<IActionResult> OnPostAddMembersAsync()
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            if (!IsGroupAdmin)
            {
                TempData["ErrorMessage"] = "Chỉ quản trị nhóm mới được thêm thành viên.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            var friendIds = await _context.Friends
                .Where(f => f.UserId == CurrentUserId)
                .Select(f => f.FriendUserId)
                .ToListAsync();

            var existingMemberIds = await _context.ConversationMembers
                .Where(cm => cm.ConversationId == ConversationId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            var newMembers = SelectedFriendIds
                .Where(id => friendIds.Contains(id) && !existingMemberIds.Contains(id))
                .Distinct()
                .ToList();

            foreach (var userId in newMembers)
            {
                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = ConversationId,
                    UserId = userId,
                    Role = "Member",
                    JoinedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "AddGroupMembers",
                $"Thêm thành viên vào nhóm conversationId={ConversationId}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã thêm thành viên vào nhóm.";
            return RedirectToPage(new { conversationId = ConversationId });
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(string memberUserId)
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            if (!IsGroupAdmin)
            {
                TempData["ErrorMessage"] = "Chỉ quản trị nhóm mới được xóa thành viên.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            if (string.IsNullOrWhiteSpace(memberUserId))
            {
                TempData["ErrorMessage"] = "Không xác định được thành viên.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            if (memberUserId == CurrentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa chính mình. Hãy dùng chức năng rời nhóm.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == ConversationId && cm.UserId == memberUserId);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thành viên trong nhóm.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            _context.ConversationMembers.Remove(member);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "RemoveGroupMember",
                $"Xóa userId={memberUserId} khỏi nhóm conversationId={ConversationId}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã xóa thành viên khỏi nhóm.";
            return RedirectToPage(new { conversationId = ConversationId });
        }

        public async Task<IActionResult> OnPostPromoteMemberAsync(string memberUserId)
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            if (!IsGroupAdmin)
            {
                TempData["ErrorMessage"] = "Chỉ quản trị nhóm mới được đổi quyền.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            if (string.IsNullOrWhiteSpace(memberUserId))
            {
                TempData["ErrorMessage"] = "Không xác định được thành viên.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == ConversationId && cm.UserId == memberUserId);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thành viên.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            if (member.UserId == CurrentUserId)
            {
                TempData["ErrorMessage"] = "Bạn đang là quản trị nhóm.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            member.Role = member.Role == "GroupAdmin" ? "Member" : "GroupAdmin";
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "ChangeGroupMemberRole",
                $"Đổi quyền userId={memberUserId} thành {member.Role} trong conversationId={ConversationId}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã cập nhật quyền thành viên.";
            return RedirectToPage(new { conversationId = ConversationId });
        }

        public async Task<IActionResult> OnPostLeaveGroupAsync()
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            var myMembership = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == ConversationId && cm.UserId == CurrentUserId);

            if (myMembership == null)
            {
                TempData["ErrorMessage"] = "Bạn không còn trong nhóm này.";
                return RedirectToPage("/Groups/Index");
            }

            var totalAdmins = await _context.ConversationMembers
                .CountAsync(cm => cm.ConversationId == ConversationId && cm.Role == "GroupAdmin");

            if (myMembership.Role == "GroupAdmin" && totalAdmins <= 1)
            {
                var totalMembers = await _context.ConversationMembers
                    .CountAsync(cm => cm.ConversationId == ConversationId);

                if (totalMembers > 1)
                {
                    TempData["ErrorMessage"] = "Bạn là quản trị nhóm cuối cùng. Hãy chuyển quyền trước khi rời nhóm.";
                    return RedirectToPage(new { conversationId = ConversationId });
                }
            }

            _context.ConversationMembers.Remove(myMembership);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "LeaveGroup",
                $"Rời nhóm conversationId={ConversationId}",
                HttpContext);

            TempData["SuccessMessage"] = "Bạn đã rời nhóm.";
            return RedirectToPage("/Groups/Index");
        }

        public async Task<IActionResult> OnPostDeleteGroupAsync()
        {
            var result = await LoadGroupDataAsync();
            if (result != null)
            {
                return result;
            }

            if (!IsGroupAdmin)
            {
                TempData["ErrorMessage"] = "Chỉ quản trị nhóm mới được xóa nhóm.";
                return RedirectToPage(new { conversationId = ConversationId });
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == ConversationId && c.Type == "Group");

            if (conversation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhóm.";
                return RedirectToPage("/Groups/Index");
            }

            var members = await _context.ConversationMembers
                .Where(cm => cm.ConversationId == ConversationId)
                .ToListAsync();

            var messages = await _context.Messages
                .Where(m => m.ConversationId == ConversationId)
                .ToListAsync();

            if (members.Any()) _context.ConversationMembers.RemoveRange(members);
            if (messages.Any()) _context.Messages.RemoveRange(messages);

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "DeleteGroup",
                $"Xóa nhóm conversationId={ConversationId}, tên nhóm={conversation.Name}",
                HttpContext);

            TempData["SuccessMessage"] = "Đã xóa nhóm chat.";
            return RedirectToPage("/Groups/Index");
        }

        private async Task<IActionResult?> LoadGroupDataAsync()
        {
            CurrentUserId = HttpContext.Session.GetString("UserId") ?? string.Empty;
            CurrentUserName = HttpContext.Session.GetString("FullName") ?? "Người dùng";

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            var membership = await _context.ConversationMembers
                .Include(cm => cm.Conversation)
                .FirstOrDefaultAsync(cm => cm.ConversationId == ConversationId && cm.UserId == CurrentUserId);

            if (membership == null || membership.Conversation == null || membership.Conversation.Type != "Group")
            {
                return RedirectToPage("/Groups/Index");
            }

            GroupName = membership.Conversation.Name ?? "Nhóm chat";
            IsGroupAdmin = membership.Role == "GroupAdmin";

            Members = await _context.ConversationMembers
                .Where(cm => cm.ConversationId == ConversationId)
                .Select(cm => new MemberVm
                {
                    UserId = cm.UserId,
                    FullName = cm.User != null ? (cm.User.FullName ?? "Người dùng") : "Người dùng",
                    Email = cm.User != null ? (cm.User.Email ?? "") : "",
                    Role = cm.Role,
                    IsCurrentUser = cm.UserId == CurrentUserId
                })
                .OrderByDescending(m => m.Role == "GroupAdmin")
                .ThenBy(m => m.FullName)
                .ToListAsync();

            Messages = await _context.Messages
                .Where(m => m.ConversationId == ConversationId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageVm
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender != null ? (m.Sender.FullName ?? "Người dùng") : "Người dùng",
                    Content = m.EncryptedContent,
                    SentAt = m.SentAt,
                    IsMine = m.SenderId == CurrentUserId
                })
                .ToListAsync();

            foreach (var msg in Messages)
            {
                msg.Content = _encryptionService.Decrypt(msg.Content);
            }

            var memberIds = Members.Select(m => m.UserId).ToList();

            AvailableFriends = await _context.Friends
                .Where(f => f.UserId == CurrentUserId && !memberIds.Contains(f.FriendUserId))
                .Select(f => new FriendVm
                {
                    UserId = f.FriendUserId,
                    FullName = f.FriendUser != null ? (f.FriendUser.FullName ?? "Người dùng") : "Người dùng",
                    Email = f.FriendUser != null ? (f.FriendUser.Email ?? "") : ""
                })
                .OrderBy(f => f.FullName)
                .ToListAsync();

            return null;
        }
    }
}