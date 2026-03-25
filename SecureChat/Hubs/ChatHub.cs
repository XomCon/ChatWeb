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
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task NotifyTyping(string roomName, string userId, bool isTyping)
        {
            await Clients.OthersInGroup(roomName).SendAsync("UserTyping", userId, isTyping);
        }

        public async Task MarkAsSeen(int conversationId, string userId)
        {
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
            // We send the plain message to clients (they will display it)
            // Or we could send the encrypted one and decrypt on client, 
            // but currently the app decrypts on server before showing in Razor pages.
            // To keep it simple for now, we send the plain message.
            await Clients.Group(roomName).SendAsync("ReceiveMessage", senderId, message, newMessage.SentAt.ToString("dd/MM/yyyy HH:mm"), newMessage.Id);
        }
    }
}