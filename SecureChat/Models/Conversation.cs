using System;
using System.Collections.Generic;

namespace SecureChat.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? Creator { get; set; }

        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}