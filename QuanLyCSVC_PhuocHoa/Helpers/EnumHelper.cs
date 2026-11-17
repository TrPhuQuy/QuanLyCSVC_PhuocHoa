namespace QuanLyCSVC_PhuocHoa.Helpers;

/// <summary>
/// Helper cung cấp hiển thị nhãn và badge cho các trạng thái dạng string trong CSDL.
/// </summary>
public static class EnumHelper
{
    // ---- Tình trạng thiết bị ----
    public static string TenTinhTrang(string? tt) => tt switch
    {
        "Đang sử dụng" => "Đang sử dụng",
        "Hư hỏng"      => "Hư hỏng",
        "Đang sửa"     => "Đang sửa chữa",
        "Thanh lý"     => "Đã thanh lý",
        _              => tt ?? "Không rõ"
    };

    public static string BadgeTinhTrang(string? tt) => tt switch
    {
        "Đang sử dụng" => "success",
        "Hư hỏng"      => "danger",
        "Đang sửa"     => "warning",
        "Thanh lý"     => "secondary",
        _              => "light"
    };

    // ---- Trạng thái phiếu ----
    public static string TenTrangThai(string? tt) => tt switch
    {
        "Chờ duyệt"       => "Chờ duyệt",
        "Đã duyệt"        => "Đã duyệt",
        "Từ chối"         => "Từ chối",
        "Hoàn thành"      => "Hoàn thành",
        "Đang thực hiện"  => "Đang thực hiện",
        "Đang sửa"        => "Đang sửa",
        "Đề nghị"         => "Đề nghị",
        "Đã thanh lý"     => "Đã thanh lý",
        _                 => tt ?? "Không rõ"
    };

    public static string BadgeTrangThai(string? tt) => tt switch
    {
        "Chờ duyệt"       => "warning",
        "Đã duyệt"        => "success",
        "Từ chối"         => "danger",
        "Hoàn thành"      => "primary",
        "Đang thực hiện"  => "info",
        "Đang sửa"        => "warning",
        "Đề nghị"         => "secondary",
        _                 => "light"
    };

    // ---- Loại phòng ----
    public static string TenLoaiPhong(string? loai) => loai ?? "Không rõ";

    // Danh sách tình trạng thiết bị
    public static List<string> DanhSachTinhTrang => new()
    {
        "Đang sử dụng", "Hư hỏng", "Đang sửa", "Thanh lý"
    };

    // Danh sách trạng thái phiếu
    public static List<string> DanhSachTrangThaiPhieu => new()
    {
        "Chờ duyệt", "Đã duyệt", "Từ chối", "Hoàn thành", "Đang thực hiện"
    };

    // ---- Tính toán Hao mòn & Khấu hao theo Thông tư 45/2018/TT-BTC & TT 23/2023/TT-BTC ----
    public record AssetDepreciationInfo(
        decimal NguyenGia,
        int NamDuaVaoSuDung,
        int SoNamSuDung,
        decimal TyLeHaoMonNam,
        decimal TyLeHaoMonLuyKe,
        decimal GiaTriHaoMonLuyKe,
        decimal GiaTriConLai,
        bool DaHetKhauHao
    );

    public static AssetDepreciationInfo TinhHaoMon(decimal? giaMua, int soLuong, int? namSanXuat, DateOnly? ngayMua, DateTime createdAt, string? tenLoai)
    {
        decimal nguyenGia = (giaMua ?? 0) * (soLuong > 0 ? soLuong : 1);
        int namDuaVaoSuDung = ngayMua?.Year ?? (namSanXuat.HasValue && namSanXuat.Value >= 2000 ? namSanXuat.Value : createdAt.Year);
        int currentYear = DateTime.Now.Year;
        int soNam = Math.Max(0, currentYear - namDuaVaoSuDung);

        // Quy định Thông tư 45/2018/TT-BTC và Thông tư 23/2023/TT-BTC Bộ Tài chính:
        // - Thiết bị tin học, điện tử, vi tính: 20%/năm (chu kỳ 5 năm)
        // - Thiết bị văn phòng, bàn ghế, nội thất: 10%/năm (chu kỳ 10 năm)
        // - Thiết bị dạy học thực hành, phòng thí nghiệm: 15%/năm (chu kỳ ~7 năm)
        decimal tyLeNam = 15m;
        var loaiLower = tenLoai?.ToLower() ?? "";
        if (loaiLower.Contains("máy tính") || loaiLower.Contains("tin học") || loaiLower.Contains("điện tử") || loaiLower.Contains("chiếu") || loaiLower.Contains("tivi"))
        {
            tyLeNam = 20m;
        }
        else if (loaiLower.Contains("bàn") || loaiLower.Contains("ghế") || loaiLower.Contains("tủ") || loaiLower.Contains("nội thất"))
        {
            tyLeNam = 10m;
        }

        decimal tyLeLuyKe = Math.Min(100m, soNam * tyLeNam);
        decimal haoMonLuyKe = Math.Round(nguyenGia * (tyLeLuyKe / 100m), 0);
        decimal giaTriConLai = Math.Max(0m, nguyenGia - haoMonLuyKe);
        bool daHetKhauHao = tyLeLuyKe >= 100m;

        return new AssetDepreciationInfo(nguyenGia, namDuaVaoSuDung, soNam, tyLeNam, tyLeLuyKe, haoMonLuyKe, giaTriConLai, daHetKhauHao);
    }
}

