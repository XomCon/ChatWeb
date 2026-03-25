using SecureChat.Data;
using SecureChat.Models;

namespace SecureChat.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task WriteLogAsync(
            string? userId,
            string action,
            string? description,
            HttpContext httpContext)
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = httpContext.Request.Headers["User-Agent"].ToString();

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Description = description,
                IpAddress = ipAddress,
                DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}