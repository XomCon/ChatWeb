using System;

namespace SecureChat.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string Role { get; set; } = "User";

        public bool IsActive { get; set; }

        public int FailedLoginCount { get; set; }

        public string? CurrentDeviceId { get; set; }

        public bool IsOnline { get; set; }

        public bool IsVerified { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}