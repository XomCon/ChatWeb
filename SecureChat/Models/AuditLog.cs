using System;

namespace SecureChat.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? IpAddress { get; set; }

        public string? DeviceInfo { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}