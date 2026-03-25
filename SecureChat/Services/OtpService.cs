using System.Security.Cryptography;

namespace SecureChat.Services
{
    public class OtpService
    {
        public string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        public DateTime GetExpiredTime(int minutes = 5)
        {
            return DateTime.Now.AddMinutes(minutes);
        }
    }
}