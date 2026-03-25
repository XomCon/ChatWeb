using System;

namespace SecureChat.Models
{
    public class Friend
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FriendUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }

        public User? FriendUser { get; set; }
    }
}