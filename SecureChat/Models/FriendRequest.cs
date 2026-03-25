using System;

namespace SecureChat.Models
{
    public class FriendRequest
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string ReceiverId { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }

        public User? Sender { get; set; }

        public User? Receiver { get; set; }
    }
}