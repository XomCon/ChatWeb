using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecureChat.Data;
using SecureChat.Models;

namespace SecureChat.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfileModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
            [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
            [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Display(Name = "Avatar URL")]
            [StringLength(500, ErrorMessage = "Avatar URL tối đa 500 ký tự.")]
            public string? AvatarUrl { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            Input = new ProfileInputModel
            {
                FullName = user.FullName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                AvatarUrl = user.AvatarUrl
            };

            Email = user.Email ?? string.Empty;
            Role = user.Role ?? "User";
            CreatedAt = user.CreatedAt;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            Email = user.Email ?? string.Empty;
            Role = user.Role ?? "User";
            CreatedAt = user.CreatedAt;

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return Page();
            }

            var newPhone = Input.PhoneNumber.Trim();

            var phoneExists = await _context.Users.AnyAsync(u =>
                u.PhoneNumber == newPhone && u.Id != userId);

            if (phoneExists)
            {
                ErrorMessage = "Số điện thoại đã được sử dụng bởi tài khoản khác.";
                return Page();
            }

            user.FullName = Input.FullName.Trim();
            user.PhoneNumber = newPhone;
            user.AvatarUrl = string.IsNullOrWhiteSpace(Input.AvatarUrl)
                ? null
                : Input.AvatarUrl.Trim();
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("FullName", user.FullName ?? "");

            SuccessMessage = "Cập nhật hồ sơ thành công.";

            return Page();
        }
    }
}