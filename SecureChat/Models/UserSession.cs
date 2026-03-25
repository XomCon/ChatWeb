using System;

namespace SecureChat.Models
{
    public class UserSession
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string DeviceId { get; set; }

        public string SessionToken { get; set; }

        public DateTime LoginAt { get; set; }

        public DateTime LastActivityAt { get; set; }

        public bool IsActive { get; set; }

        public User User { get; set; }
    }
}