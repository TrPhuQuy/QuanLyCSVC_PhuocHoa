using System;
using System.Threading.Tasks;
using QuanLyCSVC_PhuocHoa.Data;
using QuanLyCSVC_PhuocHoa.Models.Entities;

namespace QuanLyCSVC_PhuocHoa.Helpers;

public static class AuditLogHelper
{
    public static async Task LogAsync(ApplicationDbContext context, string userId, string hanhDong, string doiTuong, string? ipAddress = null)
    {
        try
        {
            // Đảm bảo UserId luôn tồn tại trong AspNetUsers để không vi phạm ràng buộc FK_AuditLog_Users
            string? validUserId = userId;
            var userExists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(context.Users, u => u.Id == validUserId);
            if (!userExists)
            {
                var fallbackUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Users);
                if (fallbackUser == null) return;
                validUserId = fallbackUser.Id;
            }

            var log = new LichSuHoatDong
            {
                UserId = validUserId,
                HanhDong = hanhDong,
                DoiTuong = doiTuong,
                ThoiGian = DateTime.Now,
                DiaChiIP = ipAddress
            };
            context.LichSuHoatDong.Add(log);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Fail silently to avoid breaking the application during audit logging failures
        }
    }
}
