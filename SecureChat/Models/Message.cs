using System;

namespace SecureChat.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string EncryptedContent { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsEdited { get; set; }

        public bool IsDeleted { get; set; }

        public Conversation? Conversation { get; set; }

        public User? Sender { get; set; }
        public string? FileUrl { get; set; }
        public string? FileType { get; set; } // image, file
        public bool IsSeen { get; set; }
    }
}