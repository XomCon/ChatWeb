using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;

namespace SecureChat.Pages
{
    public class CreateAdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateAdminModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Message { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var email = "admin@securechat.com";
            var password = "12345678";

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (admin == null)
            {
                admin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = "Administrator",
                    Email = email,
                    PhoneNumber = "0999999999",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    AvatarUrl = null,
                    Role = "Admin",
                    IsActive = true,
                    FailedLoginCount = 0,
                    CurrentDeviceId = null,
                    IsOnline = false,
                    IsVerified = true,
                    LastLoginAt = null,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                _context.Users.Add(admin);
                await _context.SaveChangesAsync();

                Message = "Đã tạo mới tài khoản admin thành công.";
            }
            else
            {
                admin.FullName = "Administrator";
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                admin.Role = "Admin";
                admin.IsActive = true;
                admin.IsVerified = true;
                admin.FailedLoginCount = 0;
                admin.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                Message = "Đã cập nhật lại tài khoản admin thành công.";
            }
        }
    }
}