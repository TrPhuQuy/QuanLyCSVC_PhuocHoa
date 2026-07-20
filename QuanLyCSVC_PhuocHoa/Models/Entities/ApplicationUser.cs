using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCSVC_PhuocHoa.Models.Entities;

public class ApplicationUser : IdentityUser
{
    // Ánh xạ sang thông tin cá nhân trong bảng NhanVien
    public virtual NhanVien? NhanVien { get; set; }

    [NotMapped]
    public string HoTen => NhanVien?.HoTen ?? UserName ?? "";

    [NotMapped]
    public string? ChucVu => NhanVien?.ChucVu;

    [NotMapped]
    public bool DangHoatDong => NhanVien?.TrangThai ?? true;

    [NotMapped]
    public int? BoMonId => NhanVien?.BoMonId;

    [NotMapped]
    public BoMon? BoMon => NhanVien?.BoMon;

    public virtual ICollection<LichSuHoatDong> LichSuHoatDong { get; set; } = new List<LichSuHoatDong>();
}
