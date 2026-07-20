namespace QuanLyCSVC_PhuocHoa.Models;

public static class AppRoles
{
    public const string Admin = "QuanTriVien";
    public const string NhanVienThietBi = "NhanVienThietBi";
    public const string GiaoVien = "GiaoVien";

    public static readonly string[] All = [Admin, NhanVienThietBi, GiaoVien];
}
