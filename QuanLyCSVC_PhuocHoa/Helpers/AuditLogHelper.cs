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
            var log = new LichSuHoatDong
            {
                UserId = userId,
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
