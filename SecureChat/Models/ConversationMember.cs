using System;

namespace SecureChat.Models
{
    public class ConversationMember
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Role { get; set; } = "Member";

        public DateTime JoinedAt { get; set; }

        public Conversation? Conversation { get; set; }

        public User? User { get; set; }
    }
}