using System;

namespace SecureChat.Models
{
    public class PendingRegistration
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}