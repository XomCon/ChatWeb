using System;

namespace SecureChat.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}