using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Tạo roles nếu chưa có
        var roles = new[] { AppRoles.Admin, AppRoles.NhanVienThietBi, AppRoles.GiaoVien };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }


        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var users = await context.Users.ToListAsync();
        var anyChanges = false;
        foreach (var user in users)
        {
            var userChanged = false;

            if (user.NormalizedUserName != user.UserName?.ToUpperInvariant())
            {
                user.NormalizedUserName = user.UserName?.ToUpperInvariant();
                userChanged = true;
            }
            if (user.NormalizedEmail != user.Email?.ToUpperInvariant())
            {
                user.NormalizedEmail = user.Email?.ToUpperInvariant();
                userChanged = true;
            }
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
                userChanged = true;
            }
            // Chỉ set mật khẩu mặc định nếu user chưa từng có mật khẩu (tài khoản mới được đồng bộ từ NhanVien).
            // KHÔNG ghi đè mật khẩu của user đã tồn tại / đã tự đổi mật khẩu.
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = passwordHasher.HashPassword(user, "Admin@123");
                userChanged = true;
            }

            if (userChanged)
            {
                context.Users.Update(user);
                anyChanges = true;
            }
        }
        if (anyChanges)
            await context.SaveChangesAsync();

        // Gán roles cho tài khoản theo chức vụ
        var nhanViens = await context.NhanVien.Include(n => n.User).ToListAsync();
        foreach (var nv in nhanViens)
        {
            if (nv.User == null) continue;
            var chucVuLower = nv.ChucVu.ToLower();
            string targetRole;

            if (chucVuLower.Contains("hiệu trưởng") || chucVuLower.Contains("phó hiệu trưởng"))
                targetRole = AppRoles.Admin;
            else if (chucVuLower.Contains("thiết bị") || chucVuLower.Contains("kế toán") || chucVuLower.Contains("văn thư"))
                targetRole = AppRoles.NhanVienThietBi;
            else
                targetRole = AppRoles.GiaoVien;

            var currentRoles = await userManager.GetRolesAsync(nv.User);
            if (!currentRoles.Contains(targetRole))
            {
                if (currentRoles.Any())
                    await userManager.RemoveFromRolesAsync(nv.User, currentRoles);
                await userManager.AddToRoleAsync(nv.User, targetRole);
            }
        }
    }
}