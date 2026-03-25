using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Pages.Chat
{
    public class PrivateModel : PageModel
    {
        // ===== Dependency Injection =====
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly EncryptionService _encryptionService;
        public PrivateModel(ApplicationDbContext context, AuditLogService auditLogService, EncryptionService encryptionService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _encryptionService = encryptionService;
        }

        // ===== Bind Properties =====
        [BindProperty(SupportsGet = true)]
        public string FriendUserId { get; set; } = string.Empty;

        [BindProperty]
        public string NewMessage { get; set; } = string.Empty;

        // ===== Data =====
        public string CurrentUserId { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public string FriendEmail { get; set; } = string.Empty;
        public int ConversationId { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public List<MessageVm> Messages { get; set; } = new();

        // ===== ViewModel =====
        public class MessageVm
        {
            public int Id { get; set; }
            public string SenderId { get; set; } = string.Empty;
            public string SenderName { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime SentAt { get; set; }
            public bool IsMine { get; set; }
        }

        // ===== GET =====
        public async Task<IActionResult> OnGetAsync()
        {
            var loadResult = await LoadConversationAsync();

            if (loadResult != null)
            {
                return loadResult;
            }

            return Page();
        }

        // ===== POST (SEND MESSAGE) =====
        public async Task<IActionResult> OnPostAsync()
        {
            var loadResult = await LoadConversationAsync();

            if (loadResult != null)
            {
                return loadResult;
            }

            if (string.IsNullOrWhiteSpace(NewMessage))
            {
                ErrorMessage = "Vui lòng nhập nội dung tin nhắn.";
                return Page();
            }

            var message = new Message
            {
                ConversationId = ConversationId,
                SenderId = CurrentUserId,
                EncryptedContent = _encryptionService.Encrypt(NewMessage.Trim()),
                SentAt = DateTime.Now,
                IsEdited = false,
                IsDeleted = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await _auditLogService.WriteLogAsync(
                CurrentUserId,
                "SendPrivateMessage",
                $"Gửi tin nhắn riêng tới userId={FriendUserId}, conversationId={ConversationId}",
                HttpContext);

            return RedirectToPage(new { friendUserId = FriendUserId });
        }

        // ===== PRIVATE METHODS =====
        private async Task<IActionResult?> LoadConversationAsync()
        {
            CurrentUserId = HttpContext.Session.GetString("UserId") ?? string.Empty;

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (string.IsNullOrEmpty(FriendUserId))
            {
                return RedirectToPage("/Chat/Index");
            }

            var isFriend = await _context.Friends
                .AnyAsync(f => f.UserId == CurrentUserId && f.FriendUserId == FriendUserId);

            if (!isFriend)
            {
                return RedirectToPage("/Friends/Index");
            }

            var friend = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == FriendUserId);

            if (friend == null)
            {
                return RedirectToPage("/Chat/Index");
            }

            FriendName = friend.FullName ?? "Người dùng";
            FriendEmail = friend.Email ?? "";

            var myConversationIds = await _context.ConversationMembers
                .Where(cm => cm.UserId == CurrentUserId)
                .Select(cm => cm.ConversationId)
                .ToListAsync();

            var friendConversationIds = await _context.ConversationMembers
                .Where(cm => cm.UserId == FriendUserId)
                .Select(cm => cm.ConversationId)
                .ToListAsync();

            var commonConversationId = await _context.Conversations
                .Where(c =>
                    c.Type == "Private" &&
                    myConversationIds.Contains(c.Id) &&
                    friendConversationIds.Contains(c.Id))
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (commonConversationId == 0)
            {
                var conversation = new Conversation
                {
                    Type = "Private",
                    Name = null,
                    CreatedBy = CurrentUserId,
                    CreatedAt = DateTime.Now
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = CurrentUserId,
                    Role = "Member",
                    JoinedAt = DateTime.Now
                });

                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = FriendUserId,
                    Role = "Member",
                    JoinedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();

                ConversationId = conversation.Id;
            }
            else
            {
                ConversationId = commonConversationId;
            }

            Messages = await _context.Messages
                .Where(m => m.ConversationId == ConversationId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageVm
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender != null
                        ? (m.Sender.FullName ?? "Người dùng")
                        : "Người dùng",
                    Content = _encryptionService.Decrypt(m.EncryptedContent),
                    SentAt = m.SentAt,
                    IsMine = m.SenderId == CurrentUserId
                })
                .ToListAsync();

            return null;
        }
    }
}