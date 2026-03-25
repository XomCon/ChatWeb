using Microsoft.EntityFrameworkCore;
using SecureChat.Data;

namespace SecureChat.Middleware
{
    public class SingleDeviceMiddleware
    {
        private readonly RequestDelegate _next;

        public SingleDeviceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var userId = context.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    context.Session.Clear();
                    context.Response.Redirect("/Account/Login");
                    return;
                }

                var cookieDeviceId = context.Request.Cookies["device_id"];

                if (string.IsNullOrEmpty(cookieDeviceId) ||
                    string.IsNullOrEmpty(user.CurrentDeviceId) ||
                    cookieDeviceId != user.CurrentDeviceId)
                {
                    user.IsOnline = false;
                    user.UpdatedAt = DateTime.Now;
                    await dbContext.SaveChangesAsync();

                    context.Session.Clear();

                    context.Response.Cookies.Delete("device_id");

                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await _next(context);
        }
    }
}