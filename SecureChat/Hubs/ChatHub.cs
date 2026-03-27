using Microsoft.AspNetCore.SignalR;
using SecureChat.Data;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;

        public ChatHub(ApplicationDbContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = true;
                    await _context.SaveChangesAsync();
                    
                    // Notify everyone that this user is now online
                    await Clients.All.SendAsync("UserStatusChanged", userId, true);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    await _context.SaveChangesAsync();

                    // Notify everyone that this user is now offline
                    await Clients.All.SendAsync("UserStatusChanged", userId, false);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(string roomName)
        {
            var httpContext = Context.GetHttpContext();
            var userId = httpContext?.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId)) return;

            // Extract conversationId from roomName (format: Conversation_{id})
            if (roomName.StartsWith("Conversation_"))
            {
                if (int.TryParse(roomName.Replace("Conversation_", ""), out int conversationId))
                {
                    var isMember = await _context.ConversationMembers
                        .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
                    
                    if (!isMember) return;
                }
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task NotifyTyping(string roomName, string userId, bool isTyping)
        {
            var httpContext = Context.GetHttpContext();
            var currentUserId = httpContext?.Session.GetString("UserId");
            if (currentUserId != userId) return;

            await Clients.OthersInGroup(roomName).SendAsync("UserTyping", userId, isTyping);
        }

        public async Task MarkAsSeen(int conversationId, string userId)
        {
            var httpContext = Context.GetHttpContext();
            var currentUserId = httpContext?.Session.GetString("UserId");
            if (currentUserId != userId) return;

            var unseenMessages = _context.Messages
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsSeen);

            foreach (var msg in unseenMessages)
            {
                msg.IsSeen = true;
            }

            await _context.SaveChangesAsync();
            await Clients.Group("Conversation_" + conversationId).SendAsync("MessagesMarkedAsSeen", conversationId, userId);
        }

        public async Task SendMessageToRoom(string roomName, string senderId, string message, int conversationId)
        {
            var httpContext = Context.GetHttpContext();
            var userIdFromSession = httpContext?.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdFromSession) || userIdFromSession != senderId) return;

            // Check membership
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == senderId);

            if (!isMember) return;

            // 1. Save message to Database
            var newMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                EncryptedContent = _encryptionService.Encrypt(message),
                SentAt = DateTime.Now,
                IsEdited = false,
                IsDeleted = false,
                IsSeen = false
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // 2. Broadcast to Group
            await Clients.Group(roomName).SendAsync("ReceiveMessage", senderId, message, newMessage.SentAt.ToString("dd/MM/yyyy HH:mm"), newMessage.Id);
        }
    }
}